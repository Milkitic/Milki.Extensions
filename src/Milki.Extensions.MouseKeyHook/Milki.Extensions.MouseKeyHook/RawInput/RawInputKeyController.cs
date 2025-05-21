using System;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Linearstar.Windows.RawInput.Native;

namespace Milki.Extensions.MouseKeyHook.RawInput;

public class RawInputKeyController : RawInputController, IKeyboardHook
{
    public event KeyboardCallback? KeyPressed;

    public RawInputKeyController()
    {
    }

    public Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        throw new NotImplementedException();
    }

    public Guid RegisterKeyDown(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        throw new NotImplementedException();
    }

    public Guid RegisterKeyUp(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        throw new NotImplementedException();
    }

    public Guid RegisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback)
    {
        throw new NotImplementedException();
    }

    public bool TryUnregisterKey(HookKeys hookKey)
    {
        throw new NotImplementedException();
    }

    public bool TryUnregisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey)
    {
        throw new NotImplementedException();
    }

    public bool TryUnregister(Guid identity)
    {
        throw new NotImplementedException();
    }

    protected override void OnKeyboardInput(RawKeyboard keyboardData)
    {
        if (keyboardData.VirutalKey >= 0xff) return;
        if (!ConvertType(keyboardData.Flags, out var keyAction)) return;

        // https://stackoverflow.com/questions/5920301/distinguish-between-left-and-right-shift-keys-using-rawinput
        int scanCode = keyboardData.ScanCode;
        scanCode |= (keyboardData.Flags & RawKeyboardFlags.KeyE0) != 0 ? 0xe000 : 0;
        scanCode |= (keyboardData.Flags & RawKeyboardFlags.KeyE1) != 0 ? 0xe100 : 0;

        HookKeys vkCode = (HookKeys)keyboardData.VirutalKey;
        Console.WriteLine(keyboardData);
        switch (vkCode)
        {
            case HookKeys.ShiftKey:
            case HookKeys.ControlKey:
            case HookKeys.Menu:
                var vkCodeMap =
                    (HookKeys)PInvoke.MapVirtualKey((uint)scanCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VSC_TO_VK_EX);
                vkCode = vkCodeMap;
                break;
            default:
                break;
        }

        KeyPressed?.Invoke(HookModifierKeys.None, vkCode, keyAction);
    }

    private bool ConvertType(RawKeyboardFlags flags, out KeyAction keyAction)
    {
        if ((flags & RawKeyboardFlags.Up) != 0)
        {
            keyAction = KeyAction.KeyUp;
            return true;
        }

        if (flags == RawKeyboardFlags.None)
        {
            keyAction = KeyAction.KeyDown;
            return true;
        }

        keyAction = KeyAction.KeyDown;
        return false;
    }
}