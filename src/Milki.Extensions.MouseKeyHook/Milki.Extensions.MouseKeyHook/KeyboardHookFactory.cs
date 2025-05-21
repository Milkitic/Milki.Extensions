using Milki.Extensions.MouseKeyHook.Internal;
using Milki.Extensions.MouseKeyHook.LowLevelHook;
using Milki.Extensions.MouseKeyHook.RawInput;

namespace Milki.Extensions.MouseKeyHook;

public static class KeyboardHookFactory
{
    public static IKeyboardHook CreateApplication()
    {
        return new KeyboardHook(false);
    }

    public static IKeyboardHook CreateGlobal()
    {
        return new KeyboardHook(true);
    }

    public static IKeyboardHook CreateRawInput()
    {
        var rawInputKeyController = new RawInputKeyController();
        rawInputKeyController.Start();
        return rawInputKeyController;
    }
}