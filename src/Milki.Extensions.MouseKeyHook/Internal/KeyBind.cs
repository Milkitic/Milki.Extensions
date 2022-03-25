using System;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal sealed class KeyBind
{
    public readonly KeyboardCallback Callback;
    public readonly Guid Identity;
    public readonly KeyBindTuple KeyBindTuple;
    public readonly bool AvoidRepeat;

    public KeyBind(Guid identity, KeyBindTuple keyBindTuple, KeyboardCallback callback, bool avoidRepeat)
    {
        Callback = callback;
        Identity = identity;
        KeyBindTuple = keyBindTuple;
        AvoidRepeat = avoidRepeat;
    }
}