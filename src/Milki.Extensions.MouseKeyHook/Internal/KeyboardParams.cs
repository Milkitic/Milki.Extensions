// ReSharper disable InconsistentNaming

using System;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal class KeyboardParams
{
    public KeyboardParams(bool isGlobal, IntPtr wParam, IntPtr lParam)
    {
        IsGlobal = isGlobal;
        WParam = wParam;
        LParam = lParam;
    }

    public bool IsGlobal { get; }
    public IntPtr WParam { get; }
    public IntPtr LParam { get; }
}