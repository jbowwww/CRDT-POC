using System;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace cli.Options;

/// <summary>
/// The <see cref="Attribute"> used to describe members in an <see cref="Options"/>-derived class.
/// </summary> 
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class OptionAttribute : Attribute, IOption
{
    public static OptionAttribute MakeDefault() => new OptionAttribute() { IsDefault = true };

    public bool IsDefault { get; init; } = false;// object.ReferenceEquals(this, OptionAttribute.Default);
    public bool IsRequired { get; init; } = true;
    public bool IsPositional { get; init; } = false;

    public bool IsNamed => Name != null; //!IsPositional;
    //  ?
    //     (HasShortName || HasLongName) ?
    //         throw new ArgumentOutOfRangeException(
    //             "IsPositional", IsPositional,
    //             "IsPostional options cannot have a name")
    //     : false : true;
    public string? Name =>
        HasShortName ?
            HasLongName ?
                throw new ArgumentOutOfRangeException("HasLongName", "HasLongName can't be true while HasShortName is true") :
                ShortName.ToString() :
            HasLongName ?
        LongName :
        null;
    public bool IsCaseSensitive { get; init; } = true;

    public int? ExplicitPosition { get; init; } = null;
    public bool HasExplicitPosition => ExplicitPosition != null;

    public char? ShortName { get; init; } = null;
    public bool HasShortName => ShortName != null;

    public string? LongName { get; init; } = null;
    public bool HasLongName => LongName != null;

    public OptionMember ToOptionMember(MemberInfo memberInfo) => new OptionMember(this, memberInfo);
}


// [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
// public sealed class OptionAttribute<T> : OptionAttribute
//     where T : IOptionParser
// {
//     // Optionally specify a typed parser
//     // public IOptionParser<T>? Parser { get; init; } = null;
// }
