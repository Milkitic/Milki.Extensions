using System;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal readonly struct KeyBindTuple : IEquatable<KeyBindTuple>
{
    public readonly HookKeys HookKeyCode;
    public readonly ModifierKeys Modifiers;

    public KeyBindTuple(ModifierKeys modifiers, HookKeys hookKeyCode)
    {
        Modifiers = modifiers;
        HookKeyCode = hookKeyCode;
    }

    public bool Equals(KeyBindTuple other)
    {
        return HookKeyCode == other.HookKeyCode && Modifiers == other.Modifiers;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeyBindTuple other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)HookKeyCode * 397) ^ (int)Modifiers;
        }
    }

    public static bool operator ==(KeyBindTuple left, KeyBindTuple right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(KeyBindTuple left, KeyBindTuple right)
    {
        return !left.Equals(right);
    }
}