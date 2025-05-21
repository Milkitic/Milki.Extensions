using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal static class KeyHelper
{
    public static HookModifierKeys GetGlobalModifiersState()
    {
        var modifiers = HookModifierKeys.None;
        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_SHIFT) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_LSHIFT) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_RSHIFT) & 0x8000) != 0)
        {
            modifiers |= HookModifierKeys.Shift;
        }

        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_CONTROL) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_LCONTROL) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_RCONTROL) & 0x8000) != 0)
        {
            modifiers |= HookModifierKeys.Control;
        }

        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_MENU) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_LMENU) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_RMENU) & 0x8000) != 0)
        {
            modifiers |= HookModifierKeys.Alt;
        }

        if ((PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_LWIN) & 0x8000) != 0 ||
            (PInvoke.GetKeyState((int)VIRTUAL_KEY.VK_RWIN) & 0x8000) != 0)
        {
            modifiers |= HookModifierKeys.WindowsKey;
        }

        return modifiers;
    }

    public static HookKeys MapActualVirtualKey(HookKeys vkCode, int scanCode)
    {
        switch (vkCode)
        {
            case HookKeys.ShiftKey:
            case HookKeys.ControlKey:
            case HookKeys.Menu:
                var mappedVk = (HookKeys)PInvoke.MapVirtualKey((uint)scanCode, MAP_VIRTUAL_KEY_TYPE.MAPVK_VSC_TO_VK_EX);
                if (mappedVk != 0 && mappedVk != vkCode) // 仅当映射成功且不同时才更新
                {
                    vkCode = mappedVk;
                    // Console.WriteLine($"Mapped VK: {vkCode} for ScanCode {scanCode}");
                }
                break;
        }

        return vkCode;
    }
}