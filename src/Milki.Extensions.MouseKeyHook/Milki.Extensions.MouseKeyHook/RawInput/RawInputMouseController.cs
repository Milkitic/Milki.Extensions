using Linearstar.Windows.RawInput.Native;

namespace Milki.Extensions.MouseKeyHook.RawInput;

public class RawInputMouseController : RawInputController, IMouseHook
{
    public event KeyboardCallback? MousePressed;

    public RawInputMouseController() : base(false, true)
    {
    }

    protected override void OnMouseInput(RawMouse obj)
    {
        if ((obj.Buttons & RawMouseButtonFlags.LeftButtonDown) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.LButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.LeftButtonUp) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.LButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.RightButtonDown) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.RButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.RightButtonUp) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.RButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.MiddleButtonDown) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.MButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.MiddleButtonUp) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.MButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.Button4Down) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.XButton1, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.Button4Up) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.XButton1, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.Button5Down) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.XButton2, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.Button5Up) != 0)
            MousePressed?.Invoke(HookModifierKeys.None, HookKeys.XButton2, KeyAction.KeyUp);
    }
}