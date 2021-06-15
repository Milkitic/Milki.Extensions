using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        public static IWavePlayer CreateDevice(out DeviceInfo actualDeviceInfo,
            in DeviceInfo? deviceInfo = null,
            SynchronizationContext? context = null)
        {
            DeviceInfo actual;
            if (deviceInfo is null) // use default profile
            {
                actual = GetDefaultDeviceInfo();
            }
            else
            {
                var hashSet = GetAllAvailableDevices();
                actual = hashSet.TryGetValue(deviceInfo, out var foundResult)
                    ? foundResult
                    : GetDefaultDeviceInfo();
            }

            IWavePlayer? device = null;
            try
            {
                Func<IWavePlayer> func;

                int safeLatency;
                switch (actual.Provider)
                {
                    case Providers.DirectSound:
                        safeLatency = 40;
                        var actual1 = actual;
                        func = () => actual1.Equals(DeviceInfo.DefaultDirectSound)
                            ? new DirectSoundOut(safeLatency)
                            : new DirectSoundOut(Guid.Parse(actual1.Id!),
                                Math.Max(actual1.Latency, safeLatency));
                        break;
                    case Providers.Wasapi:
                        safeLatency = 1;
                        if (actual.Equals(DeviceInfo.DefaultWasapi))
                        {
                            func = () => new WasapiOut(AudioClientShareMode.Shared, safeLatency);
                        }
                        else
                        {
                            var actual2 = actual;
                            func = () => new WasapiOut(actual2.MMDevice,
                                actual2.WasapiConfig?.IsExclusiveMode == true
                                    ? AudioClientShareMode.Exclusive
                                    : AudioClientShareMode.Shared, true,
                                actual2.Latency);
                        }

                        break;
                    case Providers.Asio:
                        var actual3 = actual;
                        func = () => new AsioOut(actual3.Name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(deviceInfo.Provider), actual.Provider, null);
                }

                if (context != null)
                    context.Send(_ => device = func.Invoke(), null);
                else
                    device = func.Invoke();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error while creating device. Force to use DirectSound!!");
                device?.Dispose();
                actual = DeviceInfo.DefaultDirectSound;
                if (context != null)
                    context.Send(_ => device = new DirectSoundOut(40), null);
                else
                    device = new DirectSoundOut(40);
            }

            actualDeviceInfo = actual;
            return device!;
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
