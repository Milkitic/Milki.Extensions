using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Milki.Extensions.MixPlayer.Devices;

public class DeviceDescription : IEquatable<DeviceDescription>
{
    [Description("Support types: ASIO, WASAPI, DirectSound")]
    public WavePlayerType WavePlayerType { get; set; }

    [Description("Available for ASIO, WASAPI, DirectSound (Guid)")]
    public string? DeviceId { get; set; }

    [Description("Available for WASAPI, DirectSound")]
    [IgnoreDataMember]
    public string? FriendlyName { get; set; }

    [Description("Available for WASAPI (excluded >= 3ms, non-excluded >= 0ms), DirectSound (around >= 20ms)")]
    public int Latency { get; set; }

    [Description("Available for WASAPI")]
    public bool IsExclusive { get; set; }

    public static DeviceDescription WasapiDefault { get; } = new()
    {
        WavePlayerType = WavePlayerType.WASAPI,
        FriendlyName = "WASAPI Auto"
    };

    public static DeviceDescription DirectSoundDefault { get; } = new()
    {
        WavePlayerType = WavePlayerType.DirectSound,
        DeviceId = Guid.Empty.ToString(),
        FriendlyName = "DirectSound Default"
    };

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DeviceDescription)obj);
    }

    public bool Equals(DeviceDescription? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return WavePlayerType == other.WavePlayerType && DeviceId == other.DeviceId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)WavePlayerType, DeviceId);
    }

    public static bool operator ==(DeviceDescription? left, DeviceDescription? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(DeviceDescription? left, DeviceDescription? right)
    {
        return !Equals(left, right);
    }
}