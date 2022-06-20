using System;
using System.Collections.Generic;
using System.Threading;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal class KeyboardHook : IKeyboardHook
{
    public event KeyboardCallback? KeyPressed;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly NativeHooks.LowLevelKeyboardProc _hookGlobalCallback; // Keeping alive the delegate
    private readonly NativeHooks.LowLevelKeyboardProc _hookAppCallback; // Keeping alive the delegate
    private readonly IntPtr _hookId;
    private readonly bool _isGlobal;

    private readonly Dictionary<KeyBindTuple, KeyBind> _registeredCallbacks = new();
    private readonly Dictionary<Guid, KeyBind> _registeredCallbackGuidMappings = new();
    private readonly HashSet<HookKeys> _downKeys = new();
    private readonly object _modifierKeysLock = new();
    private HookModifierKeys _hookModifierKeys = HookModifierKeys.None;

    public KeyboardHook(bool forceGlobal)
    {
        _isGlobal = forceGlobal;
        _hookGlobalCallback = HookGlobalCallback;
        _hookAppCallback = HookAppCallback;
        _hookId = forceGlobal
            ? NativeHooks.SetGlobalHook(_hookGlobalCallback)
            : NativeHooks.SetApplicationHook(_hookAppCallback);
    }

    public Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true)
    {
        return RegisterKeyCore(HookModifierKeys.None, hookKey, callback, avoidRepeat);
    }

    public Guid RegisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback)
    {
        return RegisterKeyCore(hookModifierKeys, hookKey, callback, true);
    }

    public bool TryUnregisterKey(HookKeys hookKey)
    {
        return TryUnregisterHotkey(HookModifierKeys.None, hookKey);
    }

    public bool TryUnregisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey)
    {
        if (hookModifierKeys == HookModifierKeys.None)
        {
            throw new ArgumentException("ModifierKeysIsNone");
        }

        var key = new KeyBindTuple(hookModifierKeys, hookKey);
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

    private Guid RegisterKeyCore(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat)
    {
        var keyBindTuple = new KeyBindTuple(hookModifierKeys, hookKey);
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
        var modifierKeys = _hookModifierKeys;
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

        KeyboardParamsDetail paramsDetail = new();
        KeyboardParamsDetail.GetParamsDetail(keyboardParams, ref paramsDetail);

        var modifierKey = paramsDetail.HookModifierKeys;
        var hookKey = paramsDetail.HookKey;
        if (paramsDetail.IsKeyDown)
        {
            if (modifierKey != HookModifierKeys.None)
            {
                lock (_modifierKeysLock)
                {
                    _hookModifierKeys |= modifierKey;
                }
            }

            HandleKeyPress(hookKey, KeyAction.KeyDown);
            _downKeys.Add(hookKey);
        }
        else if (paramsDetail.IsKeyUp)
        {
            if (modifierKey != HookModifierKeys.None)
            {
                lock (_modifierKeysLock)
                {
                    _hookModifierKeys &= ~modifierKey;
                }
            }

            HandleKeyPress(hookKey, KeyAction.KeyUp);
            _downKeys.Remove(hookKey);
        }
    }

    private IntPtr HookGlobalCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode != 0) // pass
        {
            return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // To prevent slowing keyboard input down, we use handle keyboard inputs in a separate thread
        ThreadPool.QueueUserWorkItem(HandleSingleKeyboardInput, new KeyboardParams(true, wParam, lParam));
        return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private IntPtr HookAppCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode != 0) // pass
        {
            return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // To prevent slowing keyboard input down, we use handle keyboard inputs in a separate thread
        ThreadPool.QueueUserWorkItem(HandleSingleKeyboardInput, new KeyboardParams(false, wParam, lParam));
        return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}