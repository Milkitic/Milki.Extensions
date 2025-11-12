// ReSharper disable InconsistentNaming

using System;

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

internal sealed class KeyboardParams
{
    public bool IsGlobal { get; private set; }
    public IntPtr WParam { get; private set; }
    public IntPtr LParam { get; private set; }

    public KeyboardParams() { }

    public void Initialize(bool isGlobal, IntPtr wParam, IntPtr lParam)
    {
        IsGlobal = isGlobal;
        WParam = wParam;
        LParam = lParam;
    }
}