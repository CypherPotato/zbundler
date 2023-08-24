using System.Runtime.Caching;
using zbundler.src;
using zbundler.src.Programs;

namespace zbundler;

partial class Build
{
    static object objWriteLock = new object();
    static bool isWatch = false;
    static List<Configuration> watchingConfigurations = new List<Configuration>();
    static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
    static MemoryCache watcherCache = new MemoryCache("watcher");

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
        if (watcherCache.Contains("watch"))
        {
            return;
        }
        watcherCache.Set("watch", true, DateTimeOffset.Now.AddMilliseconds(1000));

        CacheIO.Invalidate(e.FullPath);
        Thread.Sleep(350); // wait for file save

        if (e.Name == null) return;
        bool anyRun = false;
        PrintInfo("Building...");
        Console.Clear();
        foreach (Configuration config in watchingConfigurations)
        {
            if (config.IsExtensionIncluded(Path.GetExtension(e.Name)))
            {
                bool result = BuildConfigurations(watchingConfigurations, config.CompilationMode);
                anyRun |= result;
            }
        }
        if (anyRun)
        {
            PrintInfo($"{e.Name} built.");
        }
        else
        {
            PrintInfo("Nothing built.");
        }
    }

    public static bool BuildConfigurations(IEnumerable<Configuration> configurations, CompilationMode? filter = null)
    {
        bool any = false;
        foreach (var configuration in configurations)
        {
            if (filter != null && configuration.CompilationMode != filter)
            {
                continue;
            }
            if (watcherCache.Contains(configuration.Ref))
            {
                continue;
            }
            watcherCache.Add(configuration.Ref, true, new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMilliseconds(600) });

            PrintInfo($"Building {configuration.Label} ({configuration.CompilationMode})");
            try
            {
                switch (configuration.CompilationMode)
                {
                    case CompilationMode.CSS:
                        new CssBuilder().Build(configuration);
                        break;
                    case CompilationMode.SCSS:
                        new ScssBuilder().Build(configuration);
                        break;
                    case CompilationMode.SASS:
                        new SassBuilder().Build(configuration);
                        break;
                    case CompilationMode.JS:
                        new JsBuilder().Build(configuration);
                        break;
                }
                any = true;
            }
            catch (Exception ex)
            {
                PrintBuildError(ex.Message);
                SafeExit(1);
            }
        }
        return any;
    }

    public static void PrintMessage(ConsoleColor labelColor, string label, string message)
    {
        lock (objWriteLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(DateTime.Now.ToString("u"));
            Console.ForegroundColor = labelColor;
            Console.Write($"{label,12} ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }
    }

    public static void PrintInfo(string message) => PrintMessage(ConsoleColor.Blue, "info", message);
    public static void PrintBuildError(string message) => PrintMessage(ConsoleColor.Red, "error", message);
    public static void PrintDebugMessage(string message) => PrintMessage(ConsoleColor.Gray, "dbg", message);

    public static void SafeExit(int status)
    {
        if (!isWatch) Environment.Exit(status);
    }
}
