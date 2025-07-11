﻿using System.Runtime.InteropServices;

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

/// <summary>
///     The KeyboardHookStruct structure contains information about a low-level keyboard input event.
/// </summary>
/// <remarks>
///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct KeyboardHookStruct
{
    /// <summary>
    ///     Specifies a virtual-key code. The code must be a value in the range 1 to 254.
    /// </summary>
    public readonly int VirtualKeyCode;

    /// <summary>
    ///     Specifies a hardware scan code for the key.
    /// </summary>
    public readonly int ScanCode;

    /// <summary>
    ///     Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
    /// </summary>
    public readonly int Flags;

    /// <summary>
    ///     Specifies the Time stamp for this message.
    /// </summary>
    public readonly int Time;

    /// <summary>
    ///     Specifies extra information associated with the message.
    /// </summary>
    public readonly int ExtraInfo;
}