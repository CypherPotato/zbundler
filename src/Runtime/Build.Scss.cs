using NUglify;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static void BuildSCSS(Configuration configuration)
    {
        string lang = configuration.CompilationMode == CompilationMode.SASS ? "SASS" : "SCSS";
        PrintBuildMessage(lang, $"Building {configuration.Label}...");

        string tmpDir = Program.NormalizedCombine(Program.ExecutableDirectory, "scss-tmp");

        string? runnableName = Ext.GetExtFile("sass",
                Program.CurrentOS switch
                {
                    PlatformOS.Windows => "sass.bat",
                    PlatformOS.Linux => "sass",
                    PlatformOS.OSX or _ => "sass"
                }
            );

        string Minify(string s, string filename, string dirname)
        {
            bool isStdIn = dirname == "" || !Directory.Exists(dirname);
            Encoding consoleEncoding = Console.OutputEncoding;
            ProcessStartInfo pinfo = new ProcessStartInfo()
            {
                FileName = runnableName,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardErrorEncoding = consoleEncoding,
                StandardOutputEncoding = consoleEncoding,
                StandardInputEncoding = consoleEncoding
            };

            pinfo.ArgumentList.Add("--style=compressed");

            if (configuration.CompilationMode == CompilationMode.SASS)
            {
                pinfo.ArgumentList.Add("--indented");
            }
            else
            {
                pinfo.ArgumentList.Add("--no-indented");
            }

            if (isStdIn)
            {
                pinfo.ArgumentList.Add("--stdin");
                pinfo.ArgumentList.Add("--quiet");
            }
            else
            {
                string inOut = $"{dirname}:{tmpDir}";
                pinfo.ArgumentList.Add(inOut);
                pinfo.ArgumentList.Add("--no-source-map");
            }

            Process sn = Process.Start(pinfo)!;
            if (isStdIn)
            {
                sn.StandardInput.WriteLine(s);
                sn.StandardInput.Close();
            }

            sn.WaitForExit(TimeSpan.FromSeconds(10));

            string error = sn.StandardError.ReadToEnd();
            string output;

            if (!string.IsNullOrEmpty(error))
            {
                Build.PrintBuildError("Error raised on file " + filename + ":");
                Build.PrintBuildError(error);
                Build.Exit(6);
            }

            if (isStdIn)
            {
                output = sn.StandardOutput.ReadToEnd();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                if (!Directory.Exists(tmpDir))
                {
                    PrintBuildMessage(lang, "SCSS: no files were built.");
                    return "";
                }
                string[] outputFiles = Directory.GetFiles(tmpDir, "*.css", SearchOption.AllDirectories);
                foreach (string file in outputFiles)
                {
                    string content = File.ReadAllText(file).Trim();
                    sb.Append(content);
                }
                output = sb.ToString();
            }

            return output;
        }

        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawCssFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath, false);

        foreach (var content in files)
        {
            string minified;
            if (content.Mode == Configuration.PathValue.File)
            {
                string fileContents = File.ReadAllText(content.Value);
                minified = Minify(fileContents, Path.GetFileName(content.Value), Path.GetDirectoryName(content.Value)!);
            }
            else if (content.Mode == Configuration.PathValue.Directory)
            {
                minified = Minify("", "", content.Value);
            }
            else
            {
                string fileContents = FetchUri(content.Value);
                minified = Minify(fileContents, content.Value, "");
            }
            rawCssFiles.Append(minified);
        }

        string result = rawCssFiles.ToString();

        if (result == "") return;

        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            if (!Build.isWatch)
                PrintBuildMessage(lang, $"+ {Path.GetFileName(outputFile)}");
            File.WriteAllText(outputFile, result);
        }

        Directory.Delete(tmpDir, true);

        PrintBuildMessage(lang, "Build sucessfull!");
    }
}
