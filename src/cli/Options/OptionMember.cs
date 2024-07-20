using System;
using System.Reflection;

namespace cli.Options;

/// <summary>
/// A convenience class that - as does <see cref="OptionAttribute"/> - implements <see cref="IOption"/>.
/// <see cref="OptionAttribute"/> and its properties define the logic of how these options interact (or don't - some are mutually exclusive).
/// This class implements <see cref="IOption"/> as well, but it just defers to the <see cref="Option"/> properties.
/// with the <see cref="MemberInfo"/> of the member it is declared on.
/// This (may) also has some additional (secondary, aka calculated) properties, like <see cref="Type"/>, <see cref="IsBoolean"/>, etc. 
/// </summary>
internal class OptionMember : IOption
{
    public MemberInfo Member { get; init; }

    public OptionAttribute Option { get; init; }
    
    public bool IsPositional => Option.IsPositional;

    public bool IsRequired => Option.IsRequired;

    public string? Name => Option.Name ?? $"OptionAttribute implicit: Member=\"{Member.Name}\"";
    public bool IsNamed => Option.IsNamed;
    public bool IsCaseSensitive => Option.IsNamed;

    public char? ShortName => Option.ShortName;
    public bool HasShortName => ShortName != null;

    public string? LongName => Option.LongName;
    public bool HasLongName => LongName != null;

    public int? ExplicitPosition => Option.ExplicitPosition;
    public bool HasExplicitPosition => Option.HasExplicitPosition;

    public Type Type => Member.GetDeclaredType();

    public bool IsBoolean => Type == typeof(bool);

    internal OptionMember(OptionAttribute option, MemberInfo member)
    {
        Option = option;
        Member = member;
    }

    public OptionMemberValue ToOptionMemberValue(object value) => new OptionMemberValue(this, value);

    public override string ToString() =>
        IsPositional ?
            HasExplicitPosition ?
                $"Position=Explicit" :
                $"Position=Implicit,Member={Member.Name}" :
                $"Name={Name}";
}
