using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace zbundler;

internal class Program
{
    public static Architecture CurrentArch { get; private set; }
    public static PlatformOS CurrentOS { get; private set; }
    public static string ConfigurationFile { get; set; } = "";
    public static string CurrentDirectory { get; private set; } = "";
    public static string ExecutableDirectory { get; private set; } = "";

    static void PrintHeader()
    {
        Console.WriteLine("zbundler command line application");
        Console.WriteLine($"v.0.2");
        Console.WriteLine();
    }

    static void DisplayHelp()
    {
        Console.WriteLine("zbundler <command> [...args]\n");
        Console.WriteLine("Available commands:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  help              ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("       Display this help message.");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  watch             ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("       Starts watching the input files from the configuration file");
        Console.WriteLine("                           and compiles as soon as there is a change in the files.");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  build             ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("       Builds the distribution files to the output directory, from an");
        Console.WriteLine("                           configuration file.");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Examples: ");
        Console.WriteLine("zbundler watch dev.json");
        Console.WriteLine("zbundler build bundle.json");
    }

    static void Main(string[] args)
    {
        PrintHeader();

        CurrentArch = RuntimeInformation.ProcessArchitecture;
        CurrentDirectory = Directory.GetCurrentDirectory();
        ExecutableDirectory = AppDomain.CurrentDomain.BaseDirectory;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            CurrentOS = PlatformOS.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            CurrentOS = PlatformOS.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CurrentOS = PlatformOS.OSX;
        }
        else
        {
            Console.WriteLine("zbundler is not compatible with your operating system.");
            Environment.Exit(11);
        }

        if (args.Length < 1)
        {
            Console.WriteLine("Please enter a valid command or use the command below to get help:");
            Console.WriteLine("\nbundle4all help");
            Environment.Exit(1);
        }
        else
        {
            string firstCommand = args[0].ToLower();
            if (firstCommand == "help")
            {
                DisplayHelp();
                Environment.Exit(0);
            }
            else if (firstCommand == "build")
            {
                string relativePathToConfig;
                if (args.Length == 2)
                {
                    relativePathToConfig = args[1];
                }
                else relativePathToConfig = ".\\zbundler.json";
                string absolutePath;

                if (Path.IsPathFullyQualified(relativePathToConfig))
                {
                    absolutePath = relativePathToConfig;
                }
                else
                {
                    string currentPath = Directory.GetCurrentDirectory();
                    absolutePath = NormalizedCombine(currentPath, relativePathToConfig);
                }

                if (!File.Exists(absolutePath))
                {
                    Console.WriteLine("The specified relative or absolute path to the configuration file was not found.");
                    Environment.Exit(1);
                }

                Directory.SetCurrentDirectory(Path.GetDirectoryName(absolutePath)!);

                string contents = File.ReadAllText(absolutePath);
                Configuration[] configurations = ParseJson(contents);

                Build.BuildConfigurations(configurations);
            }
            else if (firstCommand == "watch")
            {
                string relativePathToConfig;
                if (args.Length == 2)
                {
                    relativePathToConfig = args[1];
                }
                else relativePathToConfig = ".\\zbundler.json";
                string absolutePath;

                if (Path.IsPathFullyQualified(relativePathToConfig))
                {
                    absolutePath = relativePathToConfig;
                }
                else
                {
                    string currentPath = Directory.GetCurrentDirectory();
                    absolutePath = NormalizedCombine(currentPath, relativePathToConfig);
                }

                if (!File.Exists(absolutePath))
                {
                    Console.WriteLine("The specified relative or absolute path to the configuration file was not found.");
                    Environment.Exit(1);
                }

                Directory.SetCurrentDirectory(Path.GetDirectoryName(absolutePath)!);

                string contents = File.ReadAllText(absolutePath);
                Configuration[] configurations = ParseJson(contents);

                Build.WatchConfigurations(configurations);

                Thread.Sleep(-1);
            }
        }
    }

    static Configuration[] ParseJson(string json)
    {
        Configuration[]? configurations = JsonSerializer.Deserialize<Configuration[]>(json, new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
        });

        if (configurations == null) return Array.Empty<Configuration>();

        return configurations;
    }

    // -> https://cy.proj.pw/#/blog-post?link=normalizando-path-combine.md
    public static string NormalizedCombine(params string[] paths)
    {
        if (paths.Length == 0) return "";

        bool startsWithSepChar = paths[0].StartsWith("/") || paths[0].StartsWith("\\");
        char environmentPathChar = Path.DirectorySeparatorChar;
        List<string> tokens = new List<string>();

        for (int ip = 0; ip < paths.Length; ip++)
        {
            string path = paths[ip]
                ?? throw new ArgumentNullException($"The path string at index {ip} is null.");

            string normalizedPath = path
                .Replace('/', environmentPathChar)
                .Replace('\\', environmentPathChar)
                .Trim(environmentPathChar);

            string[] pathIdentities = normalizedPath.Split(
                environmentPathChar,
                StringSplitOptions.RemoveEmptyEntries
            );

            tokens.AddRange(pathIdentities);
        }

        Stack<int> insertedIndexes = new Stack<int>();
        StringBuilder pathBuilder = new StringBuilder();
        foreach (string token in tokens)
        {
            if (token == ".")
            {
                continue;
            }
            else if (token == "..")
            {
                pathBuilder.Length = insertedIndexes.Pop();
            }
            else
            {
                insertedIndexes.Push(pathBuilder.Length);
                pathBuilder.Append(token);
                pathBuilder.Append(environmentPathChar);
            }
        }

        if (startsWithSepChar)
            pathBuilder.Insert(0, environmentPathChar);

        return pathBuilder.ToString().TrimEnd(environmentPathChar);
    }
}