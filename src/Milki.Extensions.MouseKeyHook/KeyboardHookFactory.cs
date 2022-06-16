using Milki.Extensions.MouseKeyHook.Internal;

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
}