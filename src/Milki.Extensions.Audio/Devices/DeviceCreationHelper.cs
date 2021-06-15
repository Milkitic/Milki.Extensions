using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Milki.Extensions.Audio.Devices
{
    public static class DeviceCreationHelper
    {
        private static readonly ILogger? Logger = Configuration.GetCurrentClassLogger();
        // ReSharper disable once InconsistentNaming
        private static readonly MMDeviceEnumerator _MMDeviceEnumerator;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        // ReSharper disable once InconsistentNaming
        private static readonly MMNotificationClient _MMNotificationClient;
        private static readonly object SetLock = new object();

        private static HashSet<DeviceInfo>? _cacheDeviceInfos;

        static DeviceCreationHelper()
        {
            _MMDeviceEnumerator = new MMDeviceEnumerator();
            _MMNotificationClient = new MMNotificationClient();
            _MMDeviceEnumerator.RegisterEndpointNotificationCallback(_MMNotificationClient);
        }

        private static HashSet<DeviceInfo>? CacheDeviceInfos
        {
            get
            {
                lock (SetLock) return _cacheDeviceInfos;
            }
            set
            {
                lock (SetLock) _cacheDeviceInfos = value;
            }
        }

        public static IWavePlayer CreateDevice(out DeviceInfo actualDeviceInfo, in DeviceInfo? deviceInfo = null)
        {
            if (deviceInfo is null) // use default profile
            {
                actualDeviceInfo = GetDefaultDeviceInfo();
            }
            else
            {
                var hashSet = GetAllAvailableDevices();
                actualDeviceInfo = hashSet.TryGetValue(deviceInfo, out var foundResult)
                    ? foundResult
                    : GetDefaultDeviceInfo();
            }

            IWavePlayer? device = null;
            try
            {
                int safeLatency;
                switch (actualDeviceInfo.Provider)
                {
                    case Providers.DirectSound:
                        safeLatency = 40;
                        device = actualDeviceInfo.Equals(DeviceInfo.DefaultDirectSound)
                            ? new DirectSoundOut(safeLatency)
                            : new DirectSoundOut(Guid.Parse(actualDeviceInfo.Id!),
                                Math.Max(actualDeviceInfo.Latency, safeLatency));
                        break;
                    case Providers.Wasapi:
                        safeLatency = 1;
                        if (actualDeviceInfo.Equals(DeviceInfo.DefaultWasapi))
                        {
                            device = new WasapiOut(AudioClientShareMode.Shared, safeLatency);
                        }
                        else
                        {
                            device = new WasapiOut(actualDeviceInfo.MMDevice,
                                actualDeviceInfo.WasapiConfig?.IsExclusiveMode == true
                                    ? AudioClientShareMode.Exclusive
                                    : AudioClientShareMode.Shared, true,
                                actualDeviceInfo.Latency);
                        }

                        break;
                    case Providers.Asio:
                        device = new AsioOut(actualDeviceInfo.Name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(deviceInfo.Provider), actualDeviceInfo.Provider, null);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error while creating device. Force to use DirectSound!!");
                device?.Dispose();
                actualDeviceInfo = DeviceInfo.DefaultDirectSound;
                device = new DirectSoundOut(40);
            }

            return device;
        }

        private static DeviceInfo GetDefaultDeviceInfo()
        {
            DeviceInfo deviceInfo;
            if (_MMDeviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                deviceInfo = DeviceInfo.DefaultWasapi;
                Logger?.LogInformation("The output device in app's config was not detected in this system, use WASAPI default.");
            }
            else
            {
                deviceInfo = DeviceInfo.DefaultDirectSound;
                Logger.LogWarning("The output device in app's config was not detected " +
                                  "or no output device detected in this system, use DirectSoundOut default!!!");
            }

            return deviceInfo;
        }

        public static HashSet<DeviceInfo> GetAllAvailableDevices()
        {
            if (CacheDeviceInfos != null)
                return CacheDeviceInfos;
            var result = EnumerateAvailableDevices().ToHashSet();
            CacheDeviceInfos = result;
            return result;
        }

        private static IEnumerable<DeviceInfo> EnumerateAvailableDevices()
        {
            yield return DeviceInfo.DefaultWasapi;

            foreach (var dev in DirectSoundOut.Devices)
            {
                DeviceInfo? info = null;
                try
                {
                    info = new DeviceInfo(Providers.DirectSound, dev.Guid.ToString(), dev.Description);
                }
                catch (Exception ex)
                {
                    Logger?.LogError("Error while enumerating DirectSoundOut device: {0}", ex.Message);
                }

                if (info != null)
                {
                    yield return info;
                }
            }

            foreach (var wasapi in _MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                DeviceInfo? info = null;
                try
                {
                    if (wasapi.DataFlow != DataFlow.Render || wasapi.State != DeviceState.Active) continue;
                    info = new DeviceInfo(Providers.Wasapi, wasapi.ID, wasapi.FriendlyName)
                    {
                        MMDevice = wasapi
                    };
                }
                catch (Exception ex)
                {
                    Logger?.LogError("Error while enumerating WASAPI device: {0}", ex.Message);
                }

                if (info != null)
                {
                    yield return info;
                }
            }

            foreach (var asio in AsioOut.GetDriverNames())
            {
                DeviceInfo? info = null;
                try
                {
                    info = new DeviceInfo(Providers.Asio, null, asio);
                }
                catch (Exception ex)
                {
                    Logger?.LogError("Error while enumerating ASIO device: {0}", ex.Message);
                }

                if (info != null)
                {
                    yield return info;
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        private class MMNotificationClient : IMMNotificationClient
        {
            private static readonly ILogger? InnerLogger = Configuration.GetCurrentClassLogger();
            public MMNotificationClient()
            {
                //_realEnumerator.RegisterEndpointNotificationCallback();
                if (Environment.OSVersion.Version.Major < 6)
                {
                    throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
                }
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                CacheDeviceInfos = null;
                InnerLogger?.LogDebug("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                CacheDeviceInfos = null;
                InnerLogger?.LogDebug("OnDeviceAdded --> " + pwstrDeviceId);
            }

            public void OnDeviceRemoved(string deviceId)
            {
                CacheDeviceInfos = null;
                InnerLogger?.LogDebug("OnDeviceRemoved --> " + deviceId);
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                CacheDeviceInfos = null;
                InnerLogger?.LogDebug("OnDefaultDeviceChanged --> {0}", flow.ToString());
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                //fmtid & pid are changed to formatId and propertyId in the latest version NAudio
                //InnerLogger?.LogDebug("OnPropertyValueChanged: formatId --> {0}  propertyId --> {1}",
                //    key.formatId.ToString(), key.propertyId.ToString());
            }
        }
    }
}
