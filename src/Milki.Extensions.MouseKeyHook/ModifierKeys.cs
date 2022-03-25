using System;

namespace Milki.Extensions.MouseKeyHook;

[Flags]
public enum ModifierKeys : byte
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    WindowsKey = 8,
}