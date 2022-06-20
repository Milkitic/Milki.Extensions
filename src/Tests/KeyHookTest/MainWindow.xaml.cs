using System;
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
            //_keyboardHook = KeyboardHookFactory.CreateGlobal();
            _keyboardHook = KeyboardHookFactory.CreateApplication();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _handle = _keyboardHook.RegisterHotkey(
                HookModifierKeys.Control, HookKeys.S, (modifier, key, type) =>
                {
                    Console.WriteLine("Ctrl+S");
                }
            );
            _handle = _keyboardHook.RegisterHotkey(
                HookModifierKeys.Control, HookKeys.O, (modifier, key, type) =>
                {
                    Console.WriteLine("Ctrl+O");
                }
            );
            //_handle = _keyboardHook.RegisterKeyUp(
            //    HookKeys.O, (modifier, key, type) =>
            //    {
            //        Console.WriteLine("O");
            //    }
            //);
        }
    }
}
