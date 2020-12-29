using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC
{
    public class Arguments
    {

        public readonly string Input;
        public readonly string Name;
        public readonly bool Zip;

        private class ArgsBuilder
        {
            public string Name = "pack";
            public bool Zip = false;
            public readonly IEnumerator<string> Enumerator;

            public ArgsBuilder(IEnumerator<string> enumerator)
            {
                Enumerator = enumerator;
            }
        }

        private static (char shortName, string longName, string description, Func<ArgsBuilder, bool> apply)[] Options = new[]
        {
            (
                'z', "zip", "Produces the output as a zip rather than a directory", (Func<ArgsBuilder, bool>)(b =>
                {
                    b.Zip = true;
                    return true;
                })
            ),
            (
                'n', "name", "Specifies the name of the pack", (ArgsBuilder b) =>
                {
                    if(!b.Enumerator.MoveNext())
                    {
                        PrintHelp("Expected a name");
                        return false;
                    }

                    b.Name = b.Enumerator.Current;
                    return true;
                }
            ),
            (
                'h', "help", "Print this help message", (ArgsBuilder b) =>
                {
                    PrintHelp();
                    return false;
                }
            )
        };

        private class Option
        {
            public Func<ArgsBuilder, bool>? Apply;
            public readonly Dictionary<string, Func<ArgsBuilder, bool>> Options = new();

            public Option(Func<ArgsBuilder, bool>? apply)
            {
                Apply = apply;
            }
        }


        private static readonly Dictionary<char, Option> OptionsLookup = new();

        static Arguments()
        {
            foreach ((char shortName, string longName, _, Func<ArgsBuilder, bool> apply) in Options)
            {
                if(longName.Length < 1)
                {
                    throw new InvalidOperationException($"No long name specified for short name: {shortName}");
                }
                if(OptionsLookup.ContainsKey(shortName))
                {
                    throw new InvalidOperationException($"Duplicate short name for options: {shortName}");
                }

                var lookup = new Option(apply);
                OptionsLookup.Add(shortName, lookup);
                if(!longName.StartsWith(shortName) && !OptionsLookup.TryGetValue(longName[0], out lookup))
                {
                    lookup = new Option(null);
                    OptionsLookup.Add(longName[0], lookup);
                }

                var name = longName[1..];
                if(lookup.Options.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Duplicate long name for options: {longName}");
                }

                lookup.Options.Add(name, apply);
            }
        }

        private Arguments(string input, ArgsBuilder argsBuilder)
        {
            Input = input;
            Name = argsBuilder.Name;
            Zip = argsBuilder.Zip;
        }

        public static Arguments? Parse(string[] args)
        {
            var enumerator = ((IEnumerable<string>)args).GetEnumerator();

            if(!enumerator.MoveNext())
            {
                PrintHelp();
                return null;
            }

            string input = Path.GetFullPath(enumerator.Current);
            var argsBuilder = new ArgsBuilder(enumerator);

            while (enumerator.MoveNext())
            {
                string arg = enumerator.Current;
                if (arg.Length < 2 || arg[0] != '-')
                {
                    PrintHelp($"Unknown command line argument: {arg}");
                    return null;
                }

                if (arg[1] == '-')
                {
                    if (!OptionsLookup.TryGetValue(arg[2], out Option? option) || !option.Options.TryGetValue(arg[3..], out var apply))
                    {
                        PrintHelp($"Unknown command line argument: {arg}");
                        return null;
                    }

                    if(!apply(argsBuilder))
                    {
                        return null;
                    }
                }
                else if(arg.Length != 2)
                {
                    PrintHelp($"Single dash short arguments are always only a single character: {arg}");
                    return null;
                }
                else
                {
                    if(!OptionsLookup.TryGetValue(arg[1], out Option? option) || option.Apply is not Func<ArgsBuilder, bool> apply)
                    {
                        PrintHelp($"Unknown command line argument: {arg}");
                        return null;
                    }

                    if(!apply(argsBuilder))
                    {
                        return null;
                    }
                }
            }

            return new Arguments(input, argsBuilder);
        }

        private static void PrintHelp(string? msg = null)
        {
            var builder = new StringBuilder();

            if (msg is not null)
            {
                builder.AppendLine(msg);
            }

            builder.Append(Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName) is string exe
                ? exe
                : "Arguments:"
            );
            builder.AppendLine(" <input .wasm file> [<options>]");

            if(Options.Length > 0)
            {
                builder.AppendLine("Options:");
                foreach ((char shortName, string longName, string description, _) in Options)
                {
                    builder.AppendLine($"  -{shortName}, --{longName}");
                    builder.AppendLine($"    {description}");
                }
            }

            Console.Error.WriteLine(builder);
        }
    }
}
