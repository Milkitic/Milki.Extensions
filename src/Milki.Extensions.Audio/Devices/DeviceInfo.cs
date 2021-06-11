using System;
using NAudio.CoreAudioApi;

namespace Milki.Extensions.Audio.Devices
{
    public class DeviceInfo
    {
        public DeviceInfo(Providers provider, string? id, string name)
        {
            Provider = provider;
            Id = id;
            Name = name;
        }

        private DeviceInfo()
        {
        }

        private DeviceInfo(Providers provider)
        {
            Provider = provider;
            if (provider == Providers.DirectSound)
                Id = Guid.Empty.ToString();
        }

        public string? Id { get; set; }
        public string? Name { get; set; }
        public Providers Provider { get; set; }
        public MMDevice? MMDevice { get; set; }

        //public static DeviceInfo DefaultWaveOutEvent { get; set; } = new DeviceInfo(Providers.WaveOutEvent);

        public static DeviceInfo DefaultDirectSound { get; set; } = new DeviceInfo(Providers.DirectSound);

        public static DeviceInfo DefaultWasapi { get; set; } = new DeviceInfo(Providers.Wasapi);

        public static DeviceInfo DefaultAsio { get; set; } = new DeviceInfo(Providers.Asio);

        public override bool Equals(object obj)
        {
            if (obj is DeviceInfo deviceInfo)
                return Equals(deviceInfo);
            return false;
        }

        protected bool Equals(DeviceInfo other)
        {
            if (Provider != other.Provider) return false;

            switch (Provider)
            {
                case Providers.Asio:
                    return Name == other.Name;
                default:
                    return Id == other.Id;
            }
        }

        public override int GetHashCode()
        {
            switch (Provider)
            {
                case Providers.Asio:
                    return HashCode.Combine(Name, (int)Provider);
                default:
                    return HashCode.Combine(Id, (int)Provider);
            }
        }
    }
}