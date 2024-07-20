using System;
using System.Reflection;

namespace cli.Options;

/// <summary>
/// The <see cref="Attribute"> used to describe members in an <see cref="Options"/>-derived class.
/// </summary> 
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
internal class OptionAttribute : Attribute, IOption
{
    public static OptionAttribute MakeDefault() => new OptionAttribute() { IsDefault = true };

    public bool IsDefault { get; init; } = false;
    public bool IsRequired { get; init; } = true;
    public bool IsPositional { get; init; } = false;

    public bool IsNamed => Name != null;
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

    public bool IsList { get; init; } = false;

    public OptionMember ToOptionMember(MemberInfo memberInfo) => new OptionMember(this, memberInfo);
}
