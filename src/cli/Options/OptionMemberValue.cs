using System;
using System.Reflection;

namespace cli.Options;

internal class OptionMemberValue : OptionMember
{
    object Value { get; init; }

    internal OptionMemberValue(OptionMember optionMember, object value)
        : base(optionMember.Option, optionMember.Member)
    {
        Value = value;
    }

    internal void Apply(object options)
    {
        Member.Set(options, Value);
    }

    internal static OptionMemberValue Parse(OptionMember optionMember, string value)
    {
        if (optionMember.Member.ReflectedType == null) throw new TypeAccessException($"This literally shouldn't happen for optionMember={optionMember}");
        var optionType = optionMember.Type;
        var optionName = optionMember.Member.Name;
        object parsedValue;
        // var parseableInterfaceName = optionType.TypeInitializer.Invoke() typeof(IParsable<>).MakeGenericType(optionType).Name;
        // var parseableInterface = optionType.GetInterface(parseableInterfaceName);
        // if (parseableInterface != null)
        // {
        //     parsedValue = optionType.InvokeMember(
        //         "Parse",
        //         BindingFlags.Static | BindingFlags.InvokeMethod,
        //         null,
        //         null,
        //         new object?[] { value, null }
        //     )!;
        // }
        // else
        // {
            var parseMethod = optionType.GetMethod(
                "Parse",
                BindingFlags.Static | BindingFlags.Public,
                new Type[] { typeof(string) }/* , typeof(IFormatProvider) */
            )!;
            if (parseMethod == null)
            {
                throw new MissingMethodException($"Member \"{optionName}\": Type {optionType.Name} should have a Parse(string, IFormatProvider?) method to be used as a command-line option");
            }
            parsedValue = parseMethod.Invoke(
                value,
                BindingFlags.Instance,
                null,
                new object?[] { value/* , null */ },
                null
            )!;
        // }
        return new OptionMemberValue(optionMember, parsedValue) { Member = optionMember.Member };
    }
}
