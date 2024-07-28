using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace cli.Options;

public class OptionsParser<TOptions>
    where TOptions : new()
{
    internal static IEnumerable<OptionMember> OptionMembers = typeof(TOptions).GetMembers()
        .Where(mi => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property)
        .Select(mi => mi.ToOptionMember());

    internal static IEnumerable<OptionMember> NamedOptions = OptionMembers.Where(om => om.IsNamed);

    internal static IEnumerable<OptionMember> PositionalOptions = OptionMembers.Where(om => om.IsPositional);

    internal static Queue<OptionMember> OrderedPositionalOptions = new(
        PositionalOptions.OrderBy(om => om.ExplicitPosition ?? ++HighestOrderedPosition));
    internal static int HighestOrderedPosition = PositionalOptions.Count() == 0 ? 0 : PositionalOptions.Max(om => om.ExplicitPosition ?? 0);

    internal static bool IsShort(string arg) => !IsLong(arg) && arg.StartsWith("-");
    internal static bool IsLong(string arg) => arg.StartsWith("--");
    internal static bool IsOption(string arg) => IsShort(arg) || IsLong(arg);
    internal static bool IsValue(string arg) => char.IsAscii(arg[0]);
    
    public static TOptions Parse(string[] args)
    {
        var options = new TOptions();
        var argsQueue = new Queue<string>(args);

        Console.WriteLine(
            $"OptionsParser<{typeof(TOptions).Name}>: args={args.AsString()}\n\t" +
            $"OptionMembers={OptionMembers.AsString()}\n\tNamedOptions={NamedOptions.AsString()}" +
            $"\n\tPositionalOptions={PositionalOptions.AsString()}\n\t" +
            $"OrderedPositionalOptions={OrderedPositionalOptions.AsString()}"+
            $"HighestOrderedPosition={HighestOrderedPosition}");

        var positionalIndex = 0;
        while (argsQueue.TryDequeue(out var arg))
        {
            var optionMember =
                IsLong(arg) ? OptionMembers.FirstOrDefault(om => om.HasLongName && om.LongName == arg.Substring(2)) :
                IsShort(arg) ? OptionMembers.FirstOrDefault(om => om.HasShortName && om.ShortName == arg[1]) :
                PositionalOptions.Skip(positionalIndex).FirstOrDefault();

            if (optionMember == null)
            {
                Console.WriteLine($"Couldn't match argument arg=\"{arg}\"");
            }
            else
            {
                var optionMemberValue = OptionMemberValue.Parse(optionMember, arg);
                optionMemberValue.Apply(options);
                if (optionMember.IsPositional && !optionMember.IsList)
                {
                    positionalIndex++;
                }
            }
        }
        return options;
    }
}
