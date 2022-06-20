﻿using System;
using System.Runtime.InteropServices;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal struct KeyboardParamsDetail
{
    public HookKeys HookKey;
    public HookModifierKeys HookModifierKeys;
    public int ScanCode;
    public int Timestamp;
    public bool IsKeyDown;
    public bool IsKeyUp;
    public bool IsExtendedKey;

    public static void GetParamsDetail(KeyboardParams keyboardParams, ref KeyboardParamsDetail keyboardParamsDetail)
    {
        var lParam = keyboardParams.LParam;
        var wParam = keyboardParams.WParam;

        if (keyboardParams.IsGlobal)
        {
            var keyboardHookStruct =
                (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

            var modifierKeys = GetModifierStates();

            var keyCode = (int)wParam;
            var isKeyDown = keyCode is NativeHooks.WM_KEYDOWN or NativeHooks.WM_SYSKEYDOWN;
            var isKeyUp = keyCode is NativeHooks.WM_KEYUP or NativeHooks.WM_SYSKEYUP;

            const uint maskExtendedKey = 0x1;
            var isExtendedKey = (keyboardHookStruct.Flags & maskExtendedKey) > 0;

            keyboardParamsDetail.HookKey = (HookKeys)keyboardHookStruct.VirtualKeyCode;
            keyboardParamsDetail.HookModifierKeys = modifierKeys;
            keyboardParamsDetail.ScanCode = keyboardHookStruct.ScanCode;
            keyboardParamsDetail.Timestamp = keyboardHookStruct.Time;
            keyboardParamsDetail.IsKeyDown = isKeyDown;
            keyboardParamsDetail.IsKeyUp = isKeyUp;
            keyboardParamsDetail.IsExtendedKey = isExtendedKey;
        }
        else
        {
            const uint maskKeydown = 0x40000000; // for bit 30
            const uint maskKeyup = 0x80000000; // for bit 31
            const uint maskExtendedKey = 0x1000000; // for bit 24

            var timestamp = Environment.TickCount;

            var flags = (uint)lParam.ToInt64();

            //bit 30 Specifies the previous key state. The value is 1 if the key is down before the message is sent; it is 0 if the key is up.
            var wasKeyDown = (flags & maskKeydown) > 0;
            //bit 31 Specifies the transition state. The value is 0 if the key is being pressed and 1 if it is being released.
            var isKeyReleased = (flags & maskKeyup) > 0;
            //bit 24 Specifies the extended key state. The value is 1 if the key is an extended key, otherwise the value is 0.
            var isExtendedKey = (flags & maskExtendedKey) > 0;


            var modifierKeys = GetModifierStates();
            var scanCode = (int)(((flags & 0x10000) | (flags & 0x20000) | (flags & 0x40000) | (flags & 0x80000) |
                                  (flags & 0x100000) | (flags & 0x200000) | (flags & 0x400000) | (flags & 0x800000)) >>
                                 16);

            var isKeyDown = !isKeyReleased;
            var isKeyUp = wasKeyDown && isKeyReleased;


            keyboardParamsDetail.HookKey = (HookKeys)wParam;
            keyboardParamsDetail.HookModifierKeys = modifierKeys;
            keyboardParamsDetail.ScanCode = scanCode;
            keyboardParamsDetail.Timestamp = timestamp;
            keyboardParamsDetail.IsKeyDown = isKeyDown;
            keyboardParamsDetail.IsKeyUp = isKeyUp;
            keyboardParamsDetail.IsExtendedKey = isExtendedKey;
        }
    }

    private static HookModifierKeys GetModifierStates()
    {
        var control = CheckModifier(NativeHooks.VK_CONTROL);
        var shift = CheckModifier(NativeHooks.VK_SHIFT);
        var alt = CheckModifier(NativeHooks.VK_MENU);

        return (control ? HookModifierKeys.Control : HookModifierKeys.None) |
               (shift ? HookModifierKeys.Shift : HookModifierKeys.None) |
               (alt ? HookModifierKeys.Alt : HookModifierKeys.None);
    }

    private static bool CheckModifier(int vKey)
    {
        return (NativeHooks.GetKeyState(vKey) & 0x8000) > 0;
    }
}