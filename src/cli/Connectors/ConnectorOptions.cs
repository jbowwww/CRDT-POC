using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using cli.Options;
using Ycs;

namespace cli.Connectors;

public class Options<TConnector>
    where TConnector : IConnector<TConnector>, new()
{
    internal static IEnumerable<OptionTypes OptionMembers = typeof(TConnector).GetMembers()
        .Where(om => om.MemberType == MemberTypes.Field || om.MemberType == MemberTypes.Property)
        .Select(om => (member: om, option: om.GetCustomAttribute<OptionAttribute>() ?? new OptionAttribute()));

    internal static IEnumerable<(MemberInfo member, OptionAttribute option)> NamedOptions = OptionMembers.Where(om => om.option.IsNamed);

    internal static IEnumerable<(MemberInfo member, OptionAttribute option)> PositionalOptions = OptionMembers.Where(om => om.option.IsPositional);

    internal static int HighestOrderedPosition = PositionalOptions.Max(om => om.option.ExplicitPosition ?? 0);

    internal static Queue<(MemberInfo member, OptionAttribute option)> OrderedPositionalOptions =
        new Queue<(MemberInfo member, OptionAttribute option)>(
            PositionalOptions.OrderBy(om => om.option.ExplicitPosition ?? ++HighestOrderedPosition)
        );

    internal static class StringArgumentExtensions
    {

    }
    
    internal class OptionType
    {
        public OptionAttribute Option { get; init; }

        public MemberInfo Member { get; init; }
    }

    internal class OptionInstance : OptionType
        internal OptionInstance(string flag, string? value)
        {
            _argument = argument;
            (Member, Option) = IsNamed ?
                NamedOptions.FirstOrDefault(m => m.option.Name == Name) :
                OrderedPositionalOptions.Dequeue();
        }
    }
        public bool IsShort => _argument.Length == 2 && _argument.ElementAt(0) == '-' && char.IsAsciiLetter(_argument.ElementAt(1));

        public bool IsLong => _argument.Length > 2 && _argument.StartsWith("--") && char.IsAsciiLetter(_argument.ElementAt(2));

        public bool IsNamed => IsShort || IsLong;

        public bool IsOptional => IsShort || IsLong;

        public bool IsPositional => !IsNamed;

        public string Name => IsOptional ?
            _argument.TrimStart('-') :
            throw new InvalidOperationException($"Argument \"{_argument}\" is not an option, so cannot get an OptionName");
    }
    
    internal class OptionsParser : IEnumerable<OptionInstance>
    {


        private string[] _arguments;

        internal OptionsParser(string[] arguments)
        {
            _arguments = arguments;
        }

        public IEnumerator<OptionInstance> GetEnumerator()
        {
            var arguments = new Queue(_arguments);//.AsEnumerable().GetEnumerator();
            while (arguments.Count > 0)
            {
                var argument = arguments.Dequeue();
                var option = new OptionInstance(argument);
                arguments.
                if (option.)
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public string RootDocumentName { get; init; } = "ConnectedDocument";

    public YDoc Document { get; init; } = null!;

    public Options(string[] args)
    {
        Parse(args);
    }

    public override string ToString()
    {
        return $"[{GetType().Name} RootDocumentName={RootDocumentName} Document={Document}]";
    }

    public virtual Options<TConnector> Parse(string[] args)
    {
        
        var argsIter = args.AsEnumerable<string>().GetEnumerator();
        while (argsIter.MoveNext())
        {
            var option = new OptionInstance(argsIter.Current);
            if (option.IsEmpty)
            {
                continue;
            }
            else if (option.IsNamed)//IsShort)
            {
                var option = nonPositionalOptions.SingleOrDefault(m => m.option.Name == option.OptionName.ElementAt(0));


            }
            // else if (arg.IsLong)
            // {
                // var option = FindShortOption(arg.OptionName.ElementAt(0));
            // }
            else if (arg.IsPositional)
            {
                (MemberInfo member, OptionAttribute optionAttribute) option = positionalOptions[0];
                var parser = option.optionAttribute.Parser ?? (this as IOptionParser);
                var value = parser?.Parse(arg.Argument);

                Console.WriteLine($"{arg.IsPositional}: member={option.member.Name} value={value}");

                (option.member as PropertyInfo)?.SetValue(this, value);
                (option.member as FieldInfo)?.SetValue(this, value);
                // (arg.OptionName.ElementAt())
                positionalOptions.RemoveAt(0);
            }
        }

        Console.WriteLine($"string[] args = " + string.Options(", ", args) + $"\nthis = {this}");
        return this;
    }

    public static bool TryParse(string[] args, ref T options)
    {
        if (args != null && args.Length > 0)
        {
            try
            {
                options.Parse(args);
                return true;
            }
            catch (ArgumentException /*ex*/)
            {
                // TODO: Log etc
            }
        }
        return false;
    }

    public static T? TryParse(string[] args)
    {
        T options = new();
        return TryParse(args, ref options) ? options : default;
    }
}
