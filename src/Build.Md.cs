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
        StringBuilder rawCssFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath, "*.md");

        foreach (var content in files)
        {
            string minified;
            if (content.Mode == Configuration.PathValue.File)
            {
                string fileContents = File.ReadAllText(content.Value);
                minified = Minify(fileContents, Path.GetFileName(content.Value));
            }
            else
            {
                string fileContents = FetchUri(content.Value);
                minified = Minify(fileContents, content.Value);
            }
            rawCssFiles.Append(minified);
        }

        string result = rawCssFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            if (!Build.isWatch)
                PrintBuildMessage("MD", $" ... to {Path.GetFileName(outputFile)}");
            File.WriteAllText(outputFile, result);
        }

        PrintBuildMessage("MD", "Build sucessfull!");
    }
}
