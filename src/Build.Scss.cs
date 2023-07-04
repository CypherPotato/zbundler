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

            pinfo.ArgumentList.Add("--stdin");
            pinfo.ArgumentList.Add("--quiet");
            pinfo.ArgumentList.Add("--style=compressed");

            if (configuration.CompilationMode == CompilationMode.SASS)
            {
                pinfo.ArgumentList.Add("--indented");
            }
            else
            {
                pinfo.ArgumentList.Add("--no-indented");
            }

            if (dirname != "")
            {
                pinfo.ArgumentList.Add($"--load-path={Build.EncodeParameterArgument(dirname)}");
            }

            Process sn = Process.Start(pinfo)!;
            sn.StandardInput.WriteLine(s);
            sn.StandardInput.Close();
            sn.WaitForExit();
            string output = sn.StandardOutput.ReadToEnd();
            string error = sn.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(error))
            {
                Build.PrintBuildError("Error raised on file " + filename + ":");
                Build.PrintBuildError(error);
                Build.Exit(6);
            }

            return output.TrimEnd();
        }

        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawCssFiles = new StringBuilder();
        long totalRawSizes = 0;

        string[] files = configuration.GetIncludedContents(configRelativePath, configuration.CompilationMode == CompilationMode.SASS ? "*.sass" : "*.scss");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string fileContents = File.ReadAllText(file);
            totalRawSizes += fileContents.Length;
            string dirName = Path.GetDirectoryName(file)!;
            string minified = Minify(fileContents, fileName, dirName);
            rawCssFiles.Append(minified);
        }

        if (!Build.isWatch)
            PrintBuildMessage(lang, $"Compiled to {Size.ReadableSize(totalRawSizes)} -> {Size.ReadableSize(rawCssFiles.Length)}");

        string result = rawCssFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            if (!Build.isWatch)
                PrintBuildMessage(lang, $" ... to {Path.GetFileName(outputFile)}");
            File.WriteAllText(outputFile, result);
        }

        PrintBuildMessage(lang, "Build sucessfull!");
    }
}
