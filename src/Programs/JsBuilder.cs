using NUglify;
using System.Text;

namespace zbundler.src.Programs;

internal class JsBuilder : IBuilder
{
    public BuildMode BuildMode { get; set; } = BuildMode.ManyToOne;

    public String Name => "JS";

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
            throw new Exception($"{er.Message} at line {er.StartLine}, file {er.File}");
        }

        return result.Code + ";";
    }

    public void Build(Configuration configuration)
    {
        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawJsFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath);

        foreach (var content in files)
        {
            string minified;
            if (content.Mode == Configuration.PathValue.File)
            {
                string fileContents = CacheIO.RetrieveFile(content.Value);
                minified = Minify(fileContents, Path.GetFileName(content.Value));
            }
            else
            {
                string fileContents = CacheIO.RetrieveURL(content.Value);
                minified = Minify(fileContents, content.Value);
            }
            rawJsFiles.Append(minified);
        }

        string result = rawJsFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            File.WriteAllText(outputFile, result);
        }
    }
}
