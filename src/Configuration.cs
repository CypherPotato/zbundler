﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace zbundler;

public enum CompilationMode
{
    JS,
    CSS,
    SASS,
    SCSS
}

public class Configuration
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CompilationMode CompilationMode { get; set; }
    public string? Label { get; set; }
    public string[] Include { get; set; } = Array.Empty<string>();
    public string[] Output { get; set; } = Array.Empty<string>();
    public string[]? Exclude { get; set; } = null;

    public string[] GetOutputPaths(string basePath)
    {
        List<string> output = new List<string>();
        foreach (string outputPath in this.Output)
        {
            string absPath;

            if (Path.IsPathRooted(outputPath))
            {
                absPath = outputPath;
            }
            else
            {
                absPath = Program.NormalizedCombine(basePath, outputPath);
            }

            string outputDir = Path.GetDirectoryName(absPath)!;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            output.Add(absPath);
        }
        return output.ToArray();
    }

    public string[] GetIncludedContents(string basePath, string extension)
    {
        List<string> output = new List<string>();
        foreach (string includePath in this.Include)
        {
            string absPath;

            if (Path.IsPathRooted(includePath))
            {
                absPath = includePath;
            }
            else
            {
                absPath = Program.NormalizedCombine(basePath, includePath);
            }

            if (this.IsExcluded(absPath)) continue;

            if (File.Exists(absPath))
            {
                output.Add(absPath);
            }
            else if (Directory.Exists(absPath))
            {
                foreach (string file in Directory.GetFiles(absPath, extension, SearchOption.AllDirectories))
                {
                    if (this.IsExcluded(file)) continue;
                    output.Add(file);
                }
            }
            else if (Uri.TryCreate(includePath, UriKind.Absolute, out _))
            {
                output.Add(includePath);
            }
            else
            {
                Build.PrintBuildError("Couldn't find the specified file or directory " + includePath);
                Build.Exit(3);
            }
        }
        return output.ToArray();
    }

    public bool IsExcluded(string absolutePath)
    {
        string normalizedAbsPath = absolutePath.Replace('\\', '/');
        if (this.Exclude?.Length > 0)
        {
            foreach (var item in this.Exclude)
            {
                if (normalizedAbsPath.Contains(item.Replace('\\', '/'), StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }
}