using System;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Milki.Extensions.MouseKeyHook.LowLevelHook;

internal class HookLoop
{
    private readonly Func<nint> _hookFunc;
    private readonly Action<nint> _unhookFunc;
    private Thread? _loopThread;
    private uint _threadId;

    public HookLoop(Func<nint> hookFunc, Action<nint> unhookFunc)
    {
        _hookFunc = hookFunc;
        _unhookFunc = unhookFunc;
    }

    private IntPtr _hookId = IntPtr.Zero;

    public void Start()
    {
        _loopThread = new Thread(() =>
        {
            _threadId = PInvoke.GetCurrentThreadId();
            _hookId = _hookFunc();

            RunMessageLoop();

            _unhookFunc(_hookId);
        })
        {
            IsBackground = true,
            //Priority = ThreadPriority.AboveNormal
        };

        _loopThread.Start();
    }

    public void Stop()
    {
        if (_threadId == 0) return;
        PInvoke.PostThreadMessage(_threadId, PInvoke.WM_QUIT, default, default);
        _loopThread!.Join(1000);
        _threadId = 0;
    }

    private static void RunMessageLoop()
    {
        int ret;
        while ((ret = PInvoke.GetMessage(out var msg, HWND.Null, 0, 0)) != 0)
        {
            if (ret == -1)
            {
                break;
            }

            PInvoke.TranslateMessage(msg);
            PInvoke.DispatchMessage(msg);
        }
    }

}