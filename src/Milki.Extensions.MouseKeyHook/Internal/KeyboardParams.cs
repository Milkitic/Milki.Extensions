using System;

// ReSharper disable InconsistentNaming

namespace Milki.Extensions.MouseKeyHook.Internal;

internal readonly struct KeyboardParams
{
    internal readonly IntPtr wParam;
    internal readonly int vkCode;

    public KeyboardParams(IntPtr wParam, int vkCode)
    {
        this.wParam = wParam;
        this.vkCode = vkCode;
    }
}