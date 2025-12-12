using System;
using System.Collections.Concurrent;
using Linearstar.Windows.RawInput.Native;
using Milki.Extensions.MouseKeyHook.Internal;

namespace Milki.Extensions.MouseKeyHook.RawInput;

public class RawInputMouseKeyController : RawInputController, IKeyboardHook
{
    public event KeyboardCallback? KeyPressed;

    private readonly ConcurrentDictionary<KeyBindTuple, KeyBind> _registeredCallbacks = new();
    private readonly ConcurrentDictionary<Guid, KeyBind> _registeredCallbackGuidMappings = new();
    private readonly ConcurrentDictionary<HookKeys, bool> _downKeys = new();

    public RawInputMouseKeyController() : base(true, true)
    {
    }

    protected override void OnMouseInput(RawMouse obj)
    {
        void HandleMouseAction(HookKeys key, KeyAction action)
        {
            if (action == KeyAction.KeyDown)
            {
                if (HandleKeyPress(key, HookModifierKeys.None, KeyAction.KeyDown))
                {
                    _downKeys.TryAdd(key, true);
                }
            }
            else
            {
                if (HandleKeyPress(key, HookModifierKeys.None, KeyAction.KeyUp))
                {
                    _downKeys.TryRemove(key, out _);
                }
            }
        }

        if ((obj.Buttons & RawMouseButtonFlags.LeftButtonDown) != 0)
            HandleMouseAction(HookKeys.LButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.LeftButtonUp) != 0)
            HandleMouseAction(HookKeys.LButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.RightButtonDown) != 0)
            HandleMouseAction(HookKeys.RButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.RightButtonUp) != 0)
            HandleMouseAction(HookKeys.RButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.MiddleButtonDown) != 0)
            HandleMouseAction(HookKeys.MButton, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.MiddleButtonUp) != 0)
            HandleMouseAction(HookKeys.MButton, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.Button4Down) != 0)
            HandleMouseAction(HookKeys.XButton1, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.Button4Up) != 0)
            HandleMouseAction(HookKeys.XButton1, KeyAction.KeyUp);

        if ((obj.Buttons & RawMouseButtonFlags.Button5Down) != 0)
            HandleMouseAction(HookKeys.XButton2, KeyAction.KeyDown);
        else if ((obj.Buttons & RawMouseButtonFlags.Button5Up) != 0)
            HandleMouseAction(HookKeys.XButton2, KeyAction.KeyUp);
    }

    public Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        return RegisterKeyCore(HookModifierKeys.None, hookKey, callback, avoidRepeat, null);
    }

    public Guid RegisterKeyDown(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        return RegisterKeyCore(HookModifierKeys.None, hookKey, callback, avoidRepeat, false);
    }

    public Guid RegisterKeyUp(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        return RegisterKeyCore(HookModifierKeys.None, hookKey, callback, avoidRepeat, true);
    }

    public Guid RegisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback)
    {
        return RegisterKeyCore(hookModifierKeys, hookKey, callback, true, false);
    }

    public bool TryUnregisterKey(HookKeys hookKey)
    {
        return TryUnregisterHotkey(HookModifierKeys.None, hookKey);
    }

    public bool TryUnregisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey)
    {
        var key = new KeyBindTuple(hookModifierKeys, hookKey);
        if (_registeredCallbacks.TryRemove(key, out var keyBind))
        {
            _registeredCallbackGuidMappings.TryRemove(keyBind!.Identity, out _);
            return true;
        }

        return false;
    }

    public bool TryUnregister(Guid identity)
    {
        if (!_registeredCallbackGuidMappings.TryRemove(identity, out var keyBind))
        {
            return false;
        }

        _registeredCallbacks.TryRemove(keyBind!.KeyBindTuple, out _);
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _downKeys.Clear();
        _registeredCallbacks.Clear();
        _registeredCallbackGuidMappings.Clear();
    }

    private Guid RegisterKeyCore(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback,
        bool avoidRepeat, bool? isUpOrDown)
    {
        var keyBindTuple = new KeyBindTuple(hookModifierKeys, hookKey);
        var identity = Guid.NewGuid();
        var keyBind = new KeyBind(identity, keyBindTuple, callback, avoidRepeat, isUpOrDown);

        if (!_registeredCallbacks.TryAdd(keyBindTuple, keyBind))
        {
            throw new ArgumentException("Hotkey already registered.");
        }

        _registeredCallbackGuidMappings.TryAdd(identity, keyBind);
        return identity;
    }

    private bool HandleKeyPress(HookKeys hookKey, HookModifierKeys modifierKeys, KeyAction keyAction)
    {
        KeyPressed?.Invoke(modifierKeys, hookKey, keyAction);

        var currentKey = new KeyBindTuple(modifierKeys, hookKey);
        if (!_registeredCallbacks.TryGetValue(currentKey, out var keyBind))
        {
            return false;
        }

        if (keyBind.AvoidRepeat && keyAction == KeyAction.KeyDown && _downKeys.ContainsKey(hookKey))
        {
            return false;
        }

        bool shouldInvoke = false;
        if (keyBind.IsUpOrDown == null)
        {
            shouldInvoke = true;
        }
        else if (keyBind.IsUpOrDown == true && keyAction == KeyAction.KeyUp)
        {
            shouldInvoke = true;
        }
        else if (keyBind.IsUpOrDown == false && keyAction == KeyAction.KeyDown)
        {
            shouldInvoke = true;
        }

        if (shouldInvoke)
        {
            keyBind.Callback.Invoke(modifierKeys, hookKey, keyAction);
        }

        return true;
    }

    protected override void OnKeyboardInput(RawKeyboard keyboardData)
    {
        if (keyboardData.VirutalKey >= 0xff) return;
        var keyAction = ConvertType(keyboardData.Flags);

        int scanCode = keyboardData.ScanCode;
        scanCode |= (keyboardData.Flags & RawKeyboardFlags.KeyE0) != 0 ? 0xe000 : 0;
        scanCode |= (keyboardData.Flags & RawKeyboardFlags.KeyE1) != 0 ? 0xe100 : 0;

        HookKeys vkCode = (HookKeys)keyboardData.VirutalKey;
        // Console.WriteLine($"Raw VK: {vkCode}, Raw ScanCode: {keyboardData.ScanCode}, Enhanced ScanCode: {scanCode}, Flags: {keyboardData.Flags}");

        vkCode = KeyHelper.MapActualVirtualKey(vkCode, scanCode);

        var currentGlobalModifiers = KeyHelper.GetGlobalModifiersState();

        if (keyAction == KeyAction.KeyDown)
        {
            if (HandleKeyPress(vkCode, currentGlobalModifiers, KeyAction.KeyDown))
            {
                _downKeys.TryAdd(vkCode, true);
            }
        }
        else if (keyAction == KeyAction.KeyUp)
        {
            if (HandleKeyPress(vkCode, currentGlobalModifiers, KeyAction.KeyUp))
            {
                _downKeys.TryRemove(vkCode, out _);
            }
        }
    }

    private static KeyAction ConvertType(RawKeyboardFlags flags)
    {
        return (flags & RawKeyboardFlags.Up) != 0 ? KeyAction.KeyUp : KeyAction.KeyDown;
    }
}