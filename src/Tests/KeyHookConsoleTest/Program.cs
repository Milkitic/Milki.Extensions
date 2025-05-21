using Milki.Extensions.MouseKeyHook;

namespace KeyHookConsoleTest
{
    internal class Program
    {
        private static IMouseHook? _mouseHook;

        static void Main(string[] args)
        {
            _mouseHook = MouseHookFactory.CreateRawInput();
            _mouseHook.MousePressed += MouseHook_MousePressed;
            Console.ReadLine();
        }

        private static void MouseHook_MousePressed(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyAction type)
        {
            Console.WriteLine(hookKey + " -> " + type);
        }
    }
}
