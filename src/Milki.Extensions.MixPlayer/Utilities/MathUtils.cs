using System.Collections.Generic;
using System.Linq;

namespace Milki.Extensions.MixPlayer.Utilities;

public static class MathUtils
{
    public static T Max<T>(params T[] values)
    {
        return values.Max();
        //return Max(values.AsEnumerable());
    }

    public static T Max<T>(IEnumerable<T> values)
    {
        return values.Max();
        //var def = default(T);

        //foreach (var value in values)
        //{
        //    if (Equals(def, default(T)) || def.CompareTo(value) < 0) def = value;
        //}

        //return def;
    }
}