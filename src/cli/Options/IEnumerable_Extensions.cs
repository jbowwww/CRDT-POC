using System;
using System.Collections.Generic;
using System.Linq;

public static class IEnumerable_Extensions
{
    public static string AsString<T>(this IEnumerable<T> array, Func<T, string>? mapFn = null) =>
        $"[ {string.Join(", ", mapFn != null ? array.Select(v => mapFn(v)) : array.Cast<object>())} ]";
}