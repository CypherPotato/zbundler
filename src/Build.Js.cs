using NUglify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static void BuildJS(Configuration configuration)
    {
        PrintBuildMessage("JS", $"Building {configuration.Label}...");

        string Minify(string s, string filename)
        {
            var result = Uglify.Js(s, filename, new NUglify.JavaScript.CodeSettings()
            {
                LocalRenaming = NUglify.JavaScript.LocalRenaming.KeepAll,
                ReorderScopeDeclarations = false,
                AmdSupport = true,
                AlwaysEscapeNonAscii = true
            });

            if (result.HasErrors)
            {
                var er = result.Errors[0];
                PrintBuildError($"JS minifier: {er.Message} at line {er.StartLine}, file {er.File}");
                Build.Exit(3);
            }

            return result.Code;
        }

        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawJsFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath, "*.js");

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
            rawJsFiles.AppendLine(minified);
        }

        string result = rawJsFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            if (!Build.isWatch)
                PrintBuildMessage("JS", $" ... to {Path.GetFileName(outputFile)}");
            File.WriteAllText(outputFile, result);
        }

        PrintBuildMessage("JS", "Build sucessfull!");
    }
}
