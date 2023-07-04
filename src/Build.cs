using NUglify;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static DateTime lastWatchRun = DateTime.Now;
    static bool isWatch = false;
    static List<Configuration> watchingCssConfigurations = new List<Configuration>();
    static List<Configuration> watchingScssConfigurations = new List<Configuration>();
    static List<Configuration> watchingSassConfigurations = new List<Configuration>();
    static List<Configuration> watchingJsConfigurations = new List<Configuration>();
    static Dictionary<string, string> fetchCache = new Dictionary<string, string>();

    public static void WatchConfigurations(Configuration[] configurations)
    {
        isWatch = true;
        BuildConfigurations(configurations);
        List<string> watchingPaths = new List<string>();
        foreach (var configuration in configurations)
        {
            switch (configuration.CompilationMode)
            {
                case CompilationMode.CSS:
                    watchingCssConfigurations.Add(configuration);
                    break;
                case CompilationMode.SCSS:
                    watchingScssConfigurations.Add(configuration);
                    break;
                case CompilationMode.SASS:
                    watchingSassConfigurations.Add(configuration);
                    break;
                case CompilationMode.JS:
                    watchingJsConfigurations.Add(configuration);
                    break;
            }

            string configRelativePath = Directory.GetCurrentDirectory();
            foreach (string includePath in configuration.Include)
            {
                string absPath;

                if (Path.IsPathRooted(includePath))
                {
                    absPath = includePath;
                }
                else
                {
                    absPath = Program.NormalizedCombine(configRelativePath, includePath);
                }

                if (watchingPaths.Contains(absPath)) continue;
                if (File.Exists(absPath) || Directory.Exists(absPath))
                {
                    Watch(absPath);
                    watchingPaths.Add(absPath);
                }
            }
        }
        Console.WriteLine("Watching {0} configuration(s)", configurations.Length);
    }

    static void OnChange(object sender, FileSystemEventArgs e)
    {
        if (DateTime.Now - lastWatchRun < TimeSpan.FromMilliseconds(1750))
        {
            return;
        }

        bool hasChanges =
               e.Name?.EndsWith(".css") == true
            || e.Name?.EndsWith(".scss") == true
            || e.Name?.EndsWith(".sass") == true
            || e.Name?.EndsWith(".js") == true;

        if (hasChanges)
        {
            Build.PrintInfo("Detected changes! Building...");
        }

        try
        {
            if (e.Name?.EndsWith(".css") == true) BuildConfigurations(watchingCssConfigurations);
            if (e.Name?.EndsWith(".scss") == true) BuildConfigurations(watchingScssConfigurations);
            if (e.Name?.EndsWith(".sass") == true) BuildConfigurations(watchingSassConfigurations);
            if (e.Name?.EndsWith(".js") == true) BuildConfigurations(watchingJsConfigurations);
        }
        catch { }
        finally
        {
            Build.PrintInfo("Build done!");
            lastWatchRun = DateTime.Now;
        }
    }

    public static async void Watch(string path)
    {
        await Task.Run(() =>
        {
            FileSystemWatcher watcher = new FileSystemWatcher(path);
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += OnChange;
        });
    }

    public static void BuildConfigurations(IEnumerable<Configuration> configurations)
    {
        foreach (var configuration in configurations)
        {
            switch (configuration.CompilationMode)
            {
                case CompilationMode.CSS:
                    BuildCSS(configuration);
                    break;
                case CompilationMode.SCSS or CompilationMode.SASS:
                    BuildSCSS(configuration);
                    break;
                case CompilationMode.JS:
                    BuildJS(configuration);
                    break;
            }
        }
    }

    public static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(DateTime.Now.ToString("u"));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"   {"[info]",6} ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
    }

    public static void PrintBuildMessage(string mode, string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(DateTime.Now.ToString("u"));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"   {"[" + mode + "]",6} ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
    }

    public static void PrintBuildError(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(DateTime.Now.ToString("u"));
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"   [error] ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
    }

    static string FetchUri(string uri)
    {
        if (fetchCache.ContainsKey(uri)) return fetchCache[uri];
        using (HttpClient client = new HttpClient())
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage res = client.Send(req);
            if (!res.IsSuccessStatusCode)
            {
                PrintBuildError($"Got HTTP {(int)res.StatusCode} when trying to fetch {uri}.");
                Environment.Exit(1);
                return "";
            }

            string result = res.Content.ReadAsStringAsync().Result;
            fetchCache.Add(uri, result);
            return result;
        }
    }

    public static void Exit(int status)
    {
        if (!isWatch) Environment.Exit(status);
    }

    // -> https://stackoverflow.com/a/12364234/4698166
    public static string EncodeParameterArgument(string original)
    {
        if (string.IsNullOrEmpty(original))
            return original;
        string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
        value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
        return value;
    }
}
