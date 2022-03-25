using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal class KeyboardHook : IKeyboardHook
{
    public event KeyboardCallback? KeyPressed;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeHooks.LowLevelKeyboardProc _hookCallback; // Keeping alive the delegate
    private readonly IntPtr _hookId;
    private readonly bool _isGlobal;

    private readonly Dictionary<KeyBindTuple, KeyBind> _registeredCallbacks = new();
    private readonly Dictionary<Guid, KeyBind> _registeredCallbackGuidMappings = new();
    private readonly HashSet<HookKeys> _downKeys = new();
    private readonly object _modifierKeysLock = new();
    private ModifierKeys _modifierKeys = ModifierKeys.None;

    public KeyboardHook(bool forceGlobal)
    {
        _isGlobal = forceGlobal;
        _hookCallback = HookCallback;
        _hookId = forceGlobal
            ? NativeHooks.SetGlobalHook(_hookCallback)
            : NativeHooks.SetApplicationHook(_hookCallback);
    }

    public Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        return RegisterKeyCore(ModifierKeys.None, hookKey, callback, avoidRepeat);
    }

    public Guid RegisterHotkey(ModifierKeys modifierKeys, HookKeys hookKey, KeyboardCallback callback)
    {
        return RegisterKeyCore(modifierKeys, hookKey, callback, true);
    }

    public bool TryUnregisterKey(HookKeys hookKey)
    {
        return TryUnregisterHotkey(ModifierKeys.None, hookKey);
    }

    public bool TryUnregisterHotkey(ModifierKeys modifierKeys, HookKeys hookKey)
    {
        if (modifierKeys == ModifierKeys.None)
        {
            throw new ArgumentException("ModifierKeysIsNone");
        }

        var key = new KeyBindTuple(modifierKeys, hookKey);
        var hasValue = _registeredCallbacks.TryGetValue(key, out var keyBind);
        if (hasValue)
        {
            _registeredCallbacks.Remove(key);
            _registeredCallbackGuidMappings.Remove(keyBind!.Identity);
        }

        return hasValue;
    }

    public bool TryUnregister(Guid identity)
    {
        var hasValue = _registeredCallbackGuidMappings.TryGetValue(identity, out var keyBind);
        if (!hasValue)
        {
            return false;
        }

        _registeredCallbackGuidMappings.Remove(identity);
        _registeredCallbacks.Remove(keyBind!.KeyBindTuple);
        return true;
    }

    public void Dispose()
    {
        NativeHooks.UnhookWindowsHookEx(_hookId);
        _downKeys.Clear();
    }

    private Guid RegisterKeyCore(ModifierKeys modifierKeys, HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat)
    {
        var keyBindTuple = new KeyBindTuple(modifierKeys, hookKey);
        if (_registeredCallbacks.ContainsKey(keyBindTuple))
        {
            throw new ArgumentException("Hotkey already registered.");
        }

        var identity = Guid.NewGuid();
        var keyBind = new KeyBind(identity, keyBindTuple, callback, avoidRepeat);

        _registeredCallbacks.Add(keyBindTuple, keyBind);
        _registeredCallbackGuidMappings.Add(identity, keyBind);
        return identity;
    }

    private void HandleKeyPress(HookKeys hookKey, KeyAction keyAction)
    {
        var modifierKeys = _modifierKeys;
        KeyPressed?.Invoke(modifierKeys, hookKey, keyAction);

        var currentKey = new KeyBindTuple(modifierKeys, hookKey);
        if (!_registeredCallbacks.TryGetValue(currentKey, out var keyBind))
        {
            return;
        }

        if (keyBind.AvoidRepeat && keyAction == KeyAction.KeyDown && _downKeys.Contains(hookKey))
        {
            return;
        }

        keyBind.Callback.Invoke(modifierKeys, hookKey, keyAction);
    }

    private void HandleSingleKeyboardInput(object? state)
    {
        var keyboardParams = (KeyboardParams)state!;
        var wParam = keyboardParams.wParam;
        var vkCode = keyboardParams.vkCode;

        var modifierKey = ModifierKeysUtilities.GetModifierKeyFromCode(vkCode);

        var code = (HookKeys)vkCode;
        if (wParam == (IntPtr)NativeHooks.WM_KEYDOWN || wParam == (IntPtr)NativeHooks.WM_SYSKEYDOWN)
        {
            if (modifierKey != ModifierKeys.None)
            {
                lock (_modifierKeysLock)
                {
                    _modifierKeys |= modifierKey;
                }
            }

            HandleKeyPress(code, KeyAction.KeyDown);
            _downKeys.Add(code);
        }
        else if (wParam == (IntPtr)NativeHooks.WM_KEYUP || wParam == (IntPtr)NativeHooks.WM_SYSKEYUP)
        {
            if (modifierKey != ModifierKeys.None)
            {
                lock (_modifierKeysLock)
                {
                    _modifierKeys &= ~modifierKey;
                }
            }

            HandleKeyPress(code, KeyAction.KeyUp);
            _downKeys.Remove(code);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            // To prevent slowing keyboard input down, we use handle keyboard inputs in a separate thread
            ThreadPool.QueueUserWorkItem(this.HandleSingleKeyboardInput, new KeyboardParams(wParam, vkCode));
        }

        return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}