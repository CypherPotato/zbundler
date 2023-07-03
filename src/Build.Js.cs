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

        string[] files = configuration.GetIncludedContents(configRelativePath, "*.js");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string fileContents = File.ReadAllText(file);
            string minified = Minify(fileContents, fileName);
            rawJsFiles.AppendLine(minified);
        }

        string result = rawJsFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            File.WriteAllText(outputFile, result);
        }

        PrintBuildMessage("JS", "Build sucessfull!");
    }
}
