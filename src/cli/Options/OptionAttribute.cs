using System;

namespace cli.Options;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class OptionAttribute : Attribute
{
    // option names are either:
    // a short form option specified with a single hyphen ('-') and a single character e.g. -o
    // a long form option specified with two hyphens ('--') and a string e.g. --option
    public string? Name { get; init; } = null;

    // indicates this option positional (i.e. there is no long or short flags)
    public bool IsPositional => Name == null;

    // An optional user-specified ordering/position of this option relative to the other options
    public int? ExplicitPosition { get; init; } = null;

    // indicates this option is a short named option (-C) where C is any single character
    public bool IsShort => Name?.Length == 1;

    public bool IsLong => Name?.Length >= 2 && Name.StartsWith("--") && char.IsAsciiLetter(Name[2]);

    public bool IsNamed => Name?.Length > 0;

    public bool IsCaseSensitive { get; } = true;

    public Type? Parser { get; init; } = null;
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class OptionAttribute<T> : OptionAttribute
    where T : IOptionParser
{
    // Optionally specify a typed parser
    // public IOptionParser<T>? Parser { get; init; } = null;
}
