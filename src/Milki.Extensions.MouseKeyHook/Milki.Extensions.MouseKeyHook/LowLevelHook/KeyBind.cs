﻿using System;

namespace Milki.Extensions.MouseKeyHook.Internal;

internal sealed class KeyBind
{
    public readonly KeyboardCallback Callback;
    public readonly Guid Identity;
    public readonly KeyBindTuple KeyBindTuple;
    public readonly bool AvoidRepeat;
    public readonly bool? IsUpOrDown;

    public KeyBind(Guid identity, KeyBindTuple keyBindTuple, KeyboardCallback callback, bool avoidRepeat, bool? isUpOrDown)
    {
        Callback = callback;
        Identity = identity;
        KeyBindTuple = keyBindTuple;
        AvoidRepeat = avoidRepeat;
        IsUpOrDown = isUpOrDown;
    }
}