using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

internal record struct KeyboardParamsDetail
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

            var keyCode = (uint)wParam;
            var isKeyDown = keyCode is PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN;
            var isKeyUp = keyCode is PInvoke.WM_KEYUP or PInvoke.WM_SYSKEYUP;

            const uint maskExtendedKey = 0x1;
            var isExtendedKey = (keyboardHookStruct.Flags & maskExtendedKey) > 0;

            keyboardParamsDetail.HookKey = (HookKeys)keyboardHookStruct.VirtualKeyCode;
            keyboardParamsDetail.HookModifierKeys = modifierKeys;
            if (keyboardParamsDetail.HookKey == HookKeys.ControlKey && modifierKeys == HookModifierKeys.Control)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }
            else if (keyboardParamsDetail.HookKey == HookKeys.ShiftKey && modifierKeys == HookModifierKeys.Shift)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }
            else if (keyboardParamsDetail.HookKey is HookKeys.LMenu or HookKeys.RMenu or HookKeys.Alt
                     && modifierKeys == HookModifierKeys.Alt)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }

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
            if (keyboardParamsDetail.HookKey == HookKeys.ControlKey && modifierKeys == HookModifierKeys.Control)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }
            else if (keyboardParamsDetail.HookKey == HookKeys.ShiftKey && modifierKeys == HookModifierKeys.Shift)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }
            else if (keyboardParamsDetail.HookKey is HookKeys.LMenu or HookKeys.RMenu or HookKeys.Alt
                     && modifierKeys == HookModifierKeys.Alt)
            {
                keyboardParamsDetail.HookModifierKeys = HookModifierKeys.None;
            }

            keyboardParamsDetail.ScanCode = scanCode;
            keyboardParamsDetail.Timestamp = timestamp;
            keyboardParamsDetail.IsKeyDown = isKeyDown;
            keyboardParamsDetail.IsKeyUp = isKeyUp;
            keyboardParamsDetail.IsExtendedKey = isExtendedKey;
            //Console.WriteLine(keyboardParamsDetail);
        }
    }

    private static HookModifierKeys GetModifierStates()
    {
        var control = CheckModifier(VIRTUAL_KEY.VK_CONTROL);
        var shift = CheckModifier(VIRTUAL_KEY.VK_SHIFT);
        var alt = CheckModifier(VIRTUAL_KEY.VK_MENU);

        return (control ? HookModifierKeys.Control : HookModifierKeys.None) |
               (shift ? HookModifierKeys.Shift : HookModifierKeys.None) |
               (alt ? HookModifierKeys.Alt : HookModifierKeys.None);
    }

    private static bool CheckModifier(VIRTUAL_KEY vKey)
    {
        return (PInvoke.GetKeyState((int)vKey) & 0x8000) > 0;
    }
}