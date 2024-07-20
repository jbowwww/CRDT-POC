using System;
using System.Reflection;

namespace cli.Options;

public static class MemberInfo_Extensions
{
    public static Type GetDeclaredType(this MemberInfo memberInfo) =>
        memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo)!.FieldType :
        memberInfo.MemberType == MemberTypes.Property ? (memberInfo as PropertyInfo)!.PropertyType :
            throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");

    public static void Set(this MemberInfo memberInfo, object options, object value)
    {
        if (memberInfo.MemberType == MemberTypes.Field)
            (memberInfo as FieldInfo)!.SetValue(options, value);
        else if (memberInfo.MemberType == MemberTypes.Property)
            (memberInfo as PropertyInfo)!.SetValue(options, value);
        else
            throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");
    }

    internal static OptionMember ToOptionMember(this MemberInfo member) =>
        new OptionMember(member.GetCustomAttribute<OptionAttribute>() ?? OptionAttribute.MakeDefault(), member);
}
