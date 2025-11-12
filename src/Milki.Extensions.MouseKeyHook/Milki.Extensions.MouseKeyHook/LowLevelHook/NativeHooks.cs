using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

internal static partial class NativeHooks
{
    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    internal const int WH_KEYBOARD = 2;
    internal const int WH_KEYBOARD_LL = 13;

#if NETSTANDARD2_0
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
#else
    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    internal static partial IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
#endif


    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr LoadLibrary(string lpFileName);

    internal static IntPtr SetGlobalHook(LowLevelKeyboardProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var mainModule = process.MainModule;
        if (mainModule == null)
        {
            throw new Exception("ProcMainModuleNotFound");
        }

        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, mainModule.BaseAddress, 0);
    }

    [DllImport("kernel32.dll")]
    internal static extern int GetCurrentThreadId();

    internal static IntPtr SetApplicationHook(LowLevelKeyboardProc proc)
    {
        return SetWindowsHookEx(WH_KEYBOARD, proc, IntPtr.Zero, GetCurrentThreadId());
    }
}