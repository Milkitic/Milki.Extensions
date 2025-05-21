using Milki.Extensions.MouseKeyHook;

namespace KeyHookConsoleTest
{
    internal class Program
    {
        private static IKeyboardHook? _keyboardHook;
        private static IMouseHook? _mouseHook;

        static void Main(string[] args)
        {
            _mouseHook = MouseHookFactory.CreateRawInput();
            _keyboardHook = KeyboardHookFactory.CreateRawInput();
            _mouseHook.MousePressed += MouseHook_MousePressed;
            _keyboardHook.KeyPressed += _keyboardHook_KeyPressed;
            Console.ReadLine();
        }

        private static void _keyboardHook_KeyPressed(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyAction type)
        {
            if (hookModifierKeys == HookModifierKeys.None)
            {
                Console.WriteLine(hookKey + " -> " + type);
            }
            else
            {
                Console.WriteLine(hookKey + " (" + hookModifierKeys + ")" + " -> " + type);
            }
        }

        private static void MouseHook_MousePressed(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyAction type)
        {
            Console.WriteLine(hookKey + " -> " + type);
        }
    }
}
