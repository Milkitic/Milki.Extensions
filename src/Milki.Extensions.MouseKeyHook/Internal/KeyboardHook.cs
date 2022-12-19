using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Milki.Extensions.Threading;

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
    private readonly Dictionary<HookKeys, bool> _downKeys = new();
    private readonly SingleSynchronizationContext _context;

    public KeyboardHook(bool forceGlobal)
    {
        _isGlobal = forceGlobal;
        _hookCallback = HookGlobalCallback;
        _hookId = forceGlobal
            ? NativeHooks.SetGlobalHook(_hookCallback)
            : NativeHooks.SetApplicationHook(_hookCallback);
        _context = new SingleSynchronizationContext();
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
        _context.Dispose();
        _downKeys.Clear();
    }

    private Guid RegisterKeyCore(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback,
        bool avoidRepeat, bool? isUpOrDown)
    {
        var keyBindTuple = new KeyBindTuple(hookModifierKeys, hookKey);
        if (_registeredCallbacks.ContainsKey(keyBindTuple))
        {
            throw new ArgumentException("Hotkey already registered.");
        }

        var identity = Guid.NewGuid();
        var keyBind = new KeyBind(identity, keyBindTuple, callback, avoidRepeat, isUpOrDown);

        _registeredCallbacks.Add(keyBindTuple, keyBind);
        _registeredCallbackGuidMappings.Add(identity, keyBind);
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

        if (keyBind.IsUpOrDown == true && keyAction == KeyAction.KeyUp)
        {
            keyBind.Callback.Invoke(modifierKeys, hookKey, keyAction);
        }
        if (keyBind.IsUpOrDown == false && keyAction == KeyAction.KeyDown)
        {
            keyBind.Callback.Invoke(modifierKeys, hookKey, keyAction);
        }
        else if (keyBind.IsUpOrDown == null)
        {
            keyBind.Callback.Invoke(modifierKeys, hookKey, keyAction);
        }

        return true;
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
            if (HandleKeyPress(hookKey, modifierKey, KeyAction.KeyDown))
            {
                _downKeys.Add(hookKey, true);
            }
        }
        else if (paramsDetail.IsKeyUp)
        {
            if (HandleKeyPress(hookKey, modifierKey, KeyAction.KeyUp))
            {
                _downKeys.Remove(hookKey);
            }
        }
    }

    private IntPtr HookGlobalCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode != 0) // pass
        {
            return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // To prevent slowing keyboard input down, we use handle keyboard inputs in a separate thread
        _context.Post(HandleSingleKeyboardInput, new KeyboardParams(_isGlobal, wParam, lParam));
        return NativeHooks.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}