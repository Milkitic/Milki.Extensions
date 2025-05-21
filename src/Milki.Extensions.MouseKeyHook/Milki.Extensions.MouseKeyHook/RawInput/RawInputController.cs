using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Milki.Extensions.MouseKeyHook.RawInput;

public unsafe class RawInputController : IDisposable
{
    public event Action<RawKeyboard>? KeyboardInput;
    public event Action<RawMouse>? MouseInput;
    public event Action<RawHid>? HidInput;

    private HWND _hWnd = HWND.Null;
    private string? _windowClassName;
    private ushort _classAtom;
    private Thread? _messageLoopThread;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    private WNDPROC? _wndProcDelegate;

    public RawInputController(bool registerKeyboard = true, bool registerMouse = false, bool registerController = false,
        bool registerTouch = false)
    {
        RegisterKeyboard = registerKeyboard;
        RegisterMouse = registerMouse;
        RegisterController = registerController;
        RegisterTouch = registerTouch;
    }

    public bool RegisterKeyboard { get; set; }
    public bool RegisterMouse { get; set; }
    public bool RegisterController { get; set; }
    public bool RegisterTouch { get; set; }

    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RawInputController));
        }

        if (_messageLoopThread is { IsAlive: true })
        {
            return;
        }

        _messageLoopThread = new Thread(ThreadProc)
        {
            IsBackground = true,
            Name = "RawInputMessageLoopThread"
        };
        //_messageLoopThread.SetApartmentState(ApartmentState.STA);
        _messageLoopThread.Start();
    }

    private void ThreadProc()
    {
        try
        {
            _wndProcDelegate = LpfnWndProc;
            _windowClassName = "RawInputHelperWindowClass_" + Guid.NewGuid().ToString("N");
            var hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
            if (hInstance == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get module handle.");
            }

            fixed (char* pClassName = _windowClassName)
            {
                var windowClass = new WNDCLASSW
                {
                    lpszClassName = pClassName,
                    lpfnWndProc = _wndProcDelegate,
                    hInstance = hInstance,
                };

                _classAtom = PInvoke.RegisterClass(in windowClass);
                if (_classAtom == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        $"Failed to register window class '{_windowClassName}'.");
                }
            }

            _hWnd = PInvoke.CreateWindowEx(
                WINDOW_EX_STYLE.WS_EX_NOACTIVATE,
                _windowClassName,
                "",
                WINDOW_STYLE.WS_POPUP,
                0, 0, 0, 0,
                HWND.Null,
                null,
                null,
                IntPtr.Zero.ToPointer());

            if (_hWnd == HWND.Null)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create helper window.");
            }

            //var devices = RawInputDevice.GetDevices();
            //foreach (var rawInputDevice in devices)
            //{
            //    Console.WriteLine(rawInputDevice.ManufacturerName);
            //    Console.WriteLine(rawInputDevice.ProductName);
            //    Console.WriteLine(rawInputDevice.SerialNumber);
            //    Console.WriteLine();
            //}
            if (RegisterKeyboard)
            {
                RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                    RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _hWnd);
            }

            if (RegisterMouse)
            {
                RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                    RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _hWnd);
            }

            if (RegisterController)
            {
                RawInputDevice.RegisterDevice(HidUsageAndPage.GamePad,
                    RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _hWnd);
                RawInputDevice.RegisterDevice(HidUsageAndPage.Joystick,
                    RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, _hWnd);
            }

            if (RegisterTouch)
            {
                RawInputDevice.RegisterDevice(HidUsageAndPage.Pen, RawInputDeviceFlags.None, _hWnd);
                RawInputDevice.RegisterDevice(HidUsageAndPage.TouchPad, RawInputDeviceFlags.None, _hWnd);
                RawInputDevice.RegisterDevice(HidUsageAndPage.TouchScreen, RawInputDeviceFlags.None, _hWnd);
            }

            while (!_cts.Token.IsCancellationRequested)
            {
                var result = PInvoke.GetMessage(out var msg, HWND.Null, 0, 0);
                if (result == -1)
                {
                    break;
                }

                if (result == 0)
                {
                    break;
                }

                PInvoke.TranslateMessage(msg);
                PInvoke.DispatchMessage(msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"RawInput thread error: {ex.GetType().Name} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    private LRESULT LpfnWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_INPUT:
                if (lParam.Value != IntPtr.Zero)
                {
                    RawInputHandle rawInput = (RawInputHandle)lParam.Value;
                    RawInputHeader header = User32.GetRawInputDataHeader(rawInput);
                    switch (header.Type)
                    {
                        case RawInputDeviceType.Mouse:
                            RawMouse mouseData = User32.GetRawInputMouseData(rawInput, out _);
                            OnMouseInput(mouseData);
                            break;
                        case RawInputDeviceType.Keyboard:
                            RawKeyboard keyboardData = User32.GetRawInputKeyboardData(rawInput, out _);
                            OnKeyboardInput(keyboardData);
                            break;
                        case RawInputDeviceType.Hid:
                            RawInputHidData hidData = (RawInputHidData)RawInputData.FromHandle(rawInput);
                            if (hidData is RawInputDigitizerData digitizerData)
                            {
                                Console.WriteLine(digitizerData);
                            }
                            else
                            {
                                Console.WriteLine(hidData);
                            }

                            break;
                        default:
                            throw new ArgumentException();
                    }
                }

                return new LRESULT(0);

            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                return new LRESULT(0);
        }

        return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _cts.Cancel();

            if (_hWnd != HWND.Null && _messageLoopThread is { IsAlive: true })
            {
                PInvoke.PostMessage(_hWnd, PInvoke.WM_QUIT, new WPARAM(), new LPARAM());
            }

            if (_messageLoopThread is { IsAlive: true })
            {
                if (!_messageLoopThread.Join(TimeSpan.FromSeconds(2)))
                {
                    Console.WriteLine(
                        "Warning: RawInput message loop thread did not exit gracefully within the timeout.");
                }
            }

            _messageLoopThread = null;
            _cts.Dispose();
        }

        if (_hWnd != HWND.Null)
        {
            PInvoke.DestroyWindow(_hWnd);
            _hWnd = HWND.Null;
        }

        if (_classAtom != 0)
        {
            var hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
            if (hInstance != IntPtr.Zero)
            {
                fixed (char* pClassName = _windowClassName)
                {
                    if (pClassName != null) PInvoke.UnregisterClass(pClassName, hInstance);
                }
            }

            _classAtom = 0;
            _windowClassName = null;
        }

        _wndProcDelegate = null;
        _disposed = true;
    }

    ~RawInputController()
    {
        Dispose(false);
    }

    protected virtual void OnKeyboardInput(RawKeyboard obj)
    {
        KeyboardInput?.Invoke(obj);
    }

    protected virtual void OnMouseInput(RawMouse obj)
    {
        MouseInput?.Invoke(obj);
    }

    protected virtual void OnHidInput(RawHid obj)
    {
        HidInput?.Invoke(obj);
    }
}