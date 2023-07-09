using NUglify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;

partial class Build
{
    static void BuildCSS(Configuration configuration)
    {
        PrintBuildMessage("CSS", $"Building {configuration.Label}...");

        string Minify(string s, string filename)
        {
            var result = Uglify.Css(s, filename, new NUglify.Css.CssSettings()
            {
                CommentMode = NUglify.Css.CssComment.None,
                RemoveEmptyBlocks = true
            });

            if (result.HasErrors)
            {
                var er = result.Errors[0];
                PrintBuildError($"CSS minifier: {er.Message} at line {er.StartLine}, file {er.File}");
                Build.Exit(3);
            }
            return result.Code;
        }

        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawCssFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath, "*.css");

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
                PrintBuildMessage("CSS", $" ... to {Path.GetFileName(outputFile)}");
            File.WriteAllText(outputFile, result);
        }

        PrintBuildMessage("CSS", "Build sucessfull!");
    }
}
