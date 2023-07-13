using NUglify;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static object objWriteLock = new object();
    static bool isWatch = false;
    static List<Configuration> watchingConfigurations = new List<Configuration>();
    static Dictionary<string, string> fetchCache = new Dictionary<string, string>();

    static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
    static MemoryCache cache = new MemoryCache("watcher");

    public static void WatchConfigurations(Configuration[] configurations)
    {
        isWatch = true;

        BuildConfigurations(configurations);
        List<string> watchingPaths = new List<string>();
        foreach (var configuration in configurations)
        {
            watchingConfigurations.Add(configuration);

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
                if (Directory.Exists(absPath))
                {
                    var watcher = new FileSystemWatcher(absPath);
                    watcher.EnableRaisingEvents = true;
                    watcher.IncludeSubdirectories = true;
                    watcher.Changed += new FileSystemEventHandler(OnChanged);
                    watchers.Add(watcher);
                    watchingPaths.Add(absPath);
                }
            }
        }
        Build.PrintInfo(string.Format("Watching {0} configuration(s)", configurations.Length));
        Thread.Sleep(-1);
    }

    static void OnChanged(object source, FileSystemEventArgs e)
    {
        Thread.Sleep(250);
        try
        {
            if (e.Name?.EndsWith(".css") == true) BuildConfigurations(watchingConfigurations, CompilationMode.CSS);
            if (e.Name?.EndsWith(".js") == true) BuildConfigurations(watchingConfigurations, CompilationMode.JS);
            if (e.Name?.EndsWith(".scss") == true) BuildConfigurations(watchingConfigurations, CompilationMode.SCSS);
            if (e.Name?.EndsWith(".sass") == true) BuildConfigurations(watchingConfigurations, CompilationMode.SASS);
            if (e.Name?.EndsWith(".md") == true) BuildConfigurations(watchingConfigurations, CompilationMode.MD);
        }
        catch (Exception ex)
        {
            Build.PrintBuildError($"Error caught when trying to compile {e.Name}: {ex.Message}");
        }
        finally
        {
        }
    }

    public static void BuildConfigurations(IEnumerable<Configuration> configurations, CompilationMode? filter = null)
    {
        foreach (var configuration in configurations)
        {
            if (filter != null && configuration.CompilationMode != filter)
            {
                continue;
            }
            if (cache.Contains(configuration.Ref))
            {
                continue;
            }
            cache.Add(configuration.Ref, true, new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMilliseconds(1000) });

            switch (configuration.CompilationMode)
            {
                case CompilationMode.MD:
                    BuildMd(configuration);
                    break;
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
        Console.Write($"  [error] ");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(message);
    }

    public static void PrintDebugMessage(string message)
    {
#if DEBUG
        lock (objWriteLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(DateTime.Now.ToString("u"));
            Console.Write($"    [dbg] ");
            Console.WriteLine(message);
        }
#endif
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
