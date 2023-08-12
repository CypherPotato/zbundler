using Markdig;
using NUglify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static void BuildMd(Configuration configuration)
    {
        PrintBuildMessage("MD", $"Building {configuration.Label}...");

        string Minify(string s, string filename)
        {
            try
            {
                return Markdown.ToHtml(s);
            }
            catch (Exception ex)
            {
                Build.PrintBuildError($"Error caught while compiling markdown on file {filename}: {ex.Message}");
                return "";
            }
        }

        string configRelativePath = Directory.GetCurrentDirectory();

        var files = configuration.GetIncludedContents(configRelativePath);

        foreach (var file in files)
        {
            string minified;
            if (file.Mode == Configuration.PathValue.File)
            {
                string fileContents = File.ReadAllText(file.Value);
                minified = Minify(fileContents, Path.GetFileName(file.Value));
            }
            else
            {
                string fileContents = FetchUri(file.Value);
                minified = Minify(fileContents, file.Value);
            }

            foreach (string rawOutFile in configuration.GetOutputPaths(configRelativePath))
            {
                string outputFile = Program.ExportOutputFilename(file.Value, rawOutFile);
                if (!Build.isWatch)
                    PrintBuildMessage("MD", $"+ {Path.GetFileName(outputFile)}");
                File.WriteAllText(outputFile, minified);
            }
        }

        PrintBuildMessage("MD", "Build sucessfull!");
    }
}
