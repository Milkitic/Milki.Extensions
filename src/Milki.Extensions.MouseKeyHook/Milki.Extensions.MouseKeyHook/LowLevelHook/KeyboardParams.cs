// ReSharper disable InconsistentNaming

using System;

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

internal readonly struct KeyboardParams
{
    public readonly bool IsGlobal;
    public readonly IntPtr WParam;
    public readonly IntPtr LParam;

    public KeyboardParams(bool isGlobal, IntPtr wParam, IntPtr lParam)
    {
        IsGlobal = isGlobal;
        WParam = wParam;
        LParam = lParam;
    }
}