namespace cli.Options;

/// <summary>
/// Describes an option, and gets used by <see cref="OptionAttribute"/>, <see cref="OptionMember"/> and <see cref="OptionMemberValue"/>.
/// (those refer to the [Option] attribute used on members in the <see cref="Options"/>-derived class, this attribute combined with
/// the <see cref="MemberInfo"/> of the member itself, and the attribute, the member, and the value parsed off the command-line, respectively.)
/// </summary>
public interface IOption
{
    /// <summary>
    /// Is this option required to be specified on the command line (can apply to named or positional options)
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Is this a positional option i.e. it has no name, it assumes a value based on position on the command line.
    /// </summary>
    bool IsPositional { get; }

    public string? Name { get; }

    /// <summary>
    /// Does this option have a name (if false, then it is a positional option)
    /// </summary>
    bool IsNamed { get; }
    public bool IsCaseSensitive { get; }

    /// <summary>
    /// A single-character short option name may be used to specify a value for this option on the command line.
    /// On the command line two consecutive args may specify the option value with "-<see cref="ShortName"/> (value)".
    /// </summary>
    char? ShortName { get; }
    bool HasShortName { get; }

    /// <summary>
    /// A long option name may be used to specify a value for this option on the command line.
    /// On the command line two consecutive args may specify the option value with "--<see cref="LongName"/> (value)".
    /// </summary>
    string? LongName { get; }
    bool HasLongName { get; }

    public int? ExplicitPosition { get; }
    public bool HasExplicitPosition { get; }
}