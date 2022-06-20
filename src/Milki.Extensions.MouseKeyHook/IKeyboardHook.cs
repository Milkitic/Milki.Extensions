using System;

namespace Milki.Extensions.MouseKeyHook;

public interface IKeyboardHook : IDisposable
{
    event KeyboardCallback? KeyPressed;

    Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true);
    Guid RegisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey, KeyboardCallback callback);

    bool TryUnregisterKey(HookKeys hookKey);
    bool TryUnregisterHotkey(HookModifierKeys hookModifierKeys, HookKeys hookKey);
    bool TryUnregister(Guid identity);
}