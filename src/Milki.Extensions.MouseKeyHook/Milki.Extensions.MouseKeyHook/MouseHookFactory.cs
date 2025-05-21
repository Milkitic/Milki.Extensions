using Milki.Extensions.MouseKeyHook.RawInput;

namespace Milki.Extensions.MouseKeyHook;

public static class MouseHookFactory
{
    public static IMouseHook CreateRawInput()
    {
        var rawInputKeyController = new RawInputMouseController();
        rawInputKeyController.Start();
        return rawInputKeyController;
    }
}