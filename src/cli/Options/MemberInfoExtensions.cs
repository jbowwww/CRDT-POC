using System;
using System.Collections;
using System.Reflection;

namespace cli.Options;

public static class MemberInfo_Extensions
{
    public static Type GetDeclaredType(this MemberInfo memberInfo) =>
        memberInfo.MemberType == MemberTypes.Field ? (memberInfo as FieldInfo)!.FieldType :
        memberInfo.MemberType == MemberTypes.Property ? (memberInfo as PropertyInfo)!.PropertyType :
            throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");

    public static object? Get(this MemberInfo memberInfo, object options)
    {
        return (memberInfo.MemberType == MemberTypes.Field) ? (memberInfo as FieldInfo)!.GetValue(options) :
                    (memberInfo.MemberType == MemberTypes.Property) ? (memberInfo as PropertyInfo)!.GetValue(options) :
                    throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");
    }
    public static T? Get<T>(this MemberInfo memberInfo, object options)
    {
        return (T?)((memberInfo.MemberType == MemberTypes.Field) ? (memberInfo as FieldInfo)!.GetValue(options) :
                    (memberInfo.MemberType == MemberTypes.Property) ? (memberInfo as PropertyInfo)!.GetValue(options) :
                    throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property"));
    }
    public static void AddToList(this MemberInfo memberInfo, object options, object value)
    {
        var member = memberInfo.Get<IList>(options);
        if (member == null) throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");
        member.Add(value);
    }

    public static void Set(this MemberInfo memberInfo, object options, object value)
    {
        if (memberInfo.GetDeclaredType().IsAssignableTo(typeof(IList)))
        {
            var member = memberInfo.Get<IList>(options);
            if (member == null) throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");
            member.Add(value);
        }
        else
        {
            if (memberInfo.MemberType == MemberTypes.Field)
                (memberInfo as FieldInfo)!.SetValue(options, value);
            else if (memberInfo.MemberType == MemberTypes.Property)
                (memberInfo as PropertyInfo)!.SetValue(options, value);
            else
                throw new MemberAccessException($"Member {memberInfo.Name} should be a field or property");
        }
    }

    internal static OptionMember ToOptionMember(this MemberInfo member) =>
        new OptionMember(member.GetCustomAttribute<OptionAttribute>() ?? OptionAttribute.MakeDefault(), member);
}
