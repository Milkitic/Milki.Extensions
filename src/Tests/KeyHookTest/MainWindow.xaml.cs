using System;
using System.Threading;
using System.Windows;
using Milki.Extensions.MouseKeyHook;

namespace KeyHookTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IKeyboardHook _keyboardHook;
        private Guid _handle;

        public MainWindow()
        {
            InitializeComponent();
            _keyboardHook = KeyboardHookFactory.CreateGlobal();
            //_keyboardHook = KeyboardHookFactory.CreateApplication();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
            _handle = _keyboardHook.RegisterHotkey(
                HookModifierKeys.Control, HookKeys.S, (modifier, key, type) =>
                {
                    Console.WriteLine($"[{GetThreadName()}] Ctrl+S");
                }
            );
            _handle = _keyboardHook.RegisterHotkey(
                HookModifierKeys.Control, HookKeys.O, (modifier, key, type) =>
                {
                    Console.WriteLine($"[{GetThreadName()}] Ctrl+O");
                }
            );
            _keyboardHook.RegisterKey(HookKeys.Z, (modifier, key, type) =>
            {
                if (type == KeyAction.KeyDown)
                    Console.WriteLine($"[{GetThreadName()}] Z");
            });
            _keyboardHook.RegisterKey(HookKeys.X, (modifier, key, type) =>
            {
                if (type == KeyAction.KeyDown)
                    Console.WriteLine($"[{GetThreadName()}] X");
            });
            //_handle = _keyboardHook.RegisterKeyUp(
            //    HookKeys.O, (modifier, key, type) =>
            //    {
            //        Console.WriteLine("O");
            //    }
            //);
        }

        private void KeyboardHook_KeyPressed(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyAction type)
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

        private static string GetThreadName()
        {
            return Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
        }
    }
}
