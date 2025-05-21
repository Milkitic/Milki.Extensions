namespace Milki.Extensions.MouseKeyHook;

public interface IMouseHook
{
    event KeyboardCallback? MousePressed;
}