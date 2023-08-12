using CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using zbundler.src;
using static System.Net.Mime.MediaTypeNames;

namespace zbundler;

internal class Program
{
    public static Architecture CurrentArch { get; private set; }
    public static PlatformOS CurrentOS { get; private set; }
    public static string ConfigurationFile { get; set; } = "";
    public static string CurrentDirectory { get; private set; } = "";
    public static string ExecutableDirectory { get; private set; } = "";

    static int Main(string[] args)
    {
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

        var parser = new Parser(with =>
        {
            with.AutoHelp = true;
            with.AutoVersion = true;
            with.AllowMultiInstance = true;
            with.CaseInsensitiveEnumValues = true;
            with.MaximumDisplayWidth = Console.BufferWidth;
        });

        var result = parser.ParseArguments<BuildCmd, WatchCmd, Configuration>(args);
        result
            .WithParsed<BuildCmd>(opt => BuildOpt(opt))
            .WithParsed<WatchCmd>(opt => WatchOpt(opt))
            .WithParsed<Configuration>(opt => RunOpt(opt))
            .WithNotParsed(err =>
            {
                var helpText = HelpText.AutoBuild(result, h =>
                {
                    h.AutoVersion = true;
                    h.Heading = "zbundler by cypherpotato";
                    h.Copyright = "distributed under MIT license";
                    h.AddEnumValuesToHelpText = true;

                    return h;
                });
                Console.WriteLine(helpText);
            });
        return 0;
    }

    static int RunOpt(Configuration config)
    {
        Build.BuildConfigurations(new Configuration[] { config });
        return 0;
    }

    static int WatchOpt(WatchCmd watch)
    {
        string relativePathToConfig;
        if (watch.InputConfigurationFile != null)
        {
            relativePathToConfig = watch.InputConfigurationFile;
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
            Console.WriteLine("error: the specified relative or absolute path to the configuration file was not found.");
            Environment.Exit(1);
        }

        string contents = File.ReadAllText(absolutePath);
        Configuration[] configurations = ParseJson(contents);

        Build.WatchConfigurations(configurations);

        return 0;
    }

    static int BuildOpt(BuildCmd build)
    {
        string relativePathToConfig;
        if (build.InputConfigurationFile != null)
        {
            relativePathToConfig = build.InputConfigurationFile;
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
            Console.WriteLine("error: the specified relative or absolute path to the configuration file was not found.");
            Environment.Exit(1);
        }

        string contents = File.ReadAllText(absolutePath);
        Configuration[] configurations = ParseJson(contents);

        Build.BuildConfigurations(configurations);
        return 0;
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

    public static string ExportOutputFilename(string originalFilename, string directory)
    {
        string fullName = Path.GetFileName(originalFilename);
        string fullNameWoExt = Path.GetFileNameWithoutExtension(originalFilename);
        string ext = Path.GetExtension(originalFilename);
        directory = directory.Replace("%n", fullName);
        directory = directory.Replace("%x", fullNameWoExt);
        directory = directory.Replace("%e", ext);

        return directory;
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