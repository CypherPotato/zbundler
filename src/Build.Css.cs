﻿using NUglify;
using System;
using System.Collections.Generic;
using System.Linq;
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
        long totalRawSizes = 0;

        string[] files = configuration.GetIncludedContents(configRelativePath, "*.css");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string fileContents = File.ReadAllText(file);
            totalRawSizes += fileContents.Length;
            string minified = Minify(fileContents, fileName);
            rawCssFiles.Append(minified);
        }

        if (!Build.isWatch)
            PrintBuildMessage("CSS", $"Compiled to {Size.ReadableSize(totalRawSizes)} -> {Size.ReadableSize(rawCssFiles.Length)}");

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
