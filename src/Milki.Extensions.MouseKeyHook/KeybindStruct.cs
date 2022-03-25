using System;

namespace Milki.Extensions.MouseKeyHook;

/// <summary>
/// A struct to represent a keybind (key + modifiers)
/// When the keybind struct is compared to another keybind struct, the equality is based on the
/// modifiers and the virtual key code.
/// </summary>
internal readonly struct KeybindStruct : IEquatable<KeybindStruct>
{
    public readonly Keys Key;
    public readonly ModifierKeys Modifiers;
    public readonly Guid? Identifier;

    public KeybindStruct(ModifierKeys modifiers, Keys key, Guid? identifier = null)
    {
        this.Key = key;
        this.Modifiers = modifiers;
        this.Identifier = identifier;
    }

    public bool Equals(KeybindStruct other)
    {
        return Key == other.Key && Modifiers == other.Modifiers && Nullable.Equals(Identifier, other.Identifier);
    }

    public override bool Equals(object? obj)
    {
        return obj is KeybindStruct other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Key;
            hashCode = (hashCode * 397) ^ (int)Modifiers;
            hashCode = (hashCode * 397) ^ Identifier.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(KeybindStruct left, KeybindStruct right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(KeybindStruct left, KeybindStruct right)
    {
        return !left.Equals(right);
    }
}