using System;

public static class Type_Extensions
{
    public static bool HasInterface(this Type type, string name, bool ignoreCase = false) => type.GetInterface(name, ignoreCase) != null;
}