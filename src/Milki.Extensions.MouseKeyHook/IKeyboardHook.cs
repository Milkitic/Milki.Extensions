using System;

namespace Milki.Extensions.MouseKeyHook;

public interface IKeyboardHook : IDisposable
{
    event KeyboardCallback? KeyPressed;

    Guid RegisterKey(HookKeys hookKey, KeyboardCallback callback, bool avoidRepeat = true);
    Guid RegisterHotkey(ModifierKeys modifierKeys, HookKeys hookKey, KeyboardCallback callback);

    bool TryUnregisterKey(HookKeys hookKey);
    bool TryUnregisterHotkey(ModifierKeys modifierKeys, HookKeys hookKey);
    bool TryUnregister(Guid identity);
}