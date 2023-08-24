using NUglify;
using SimpleCSS;
using System.Text;
using System.Text.Json;

namespace zbundler.src.Programs;

internal class CssBuilder : IBuilder
{
    public BuildMode BuildMode { get; set; } = BuildMode.ManyToOne;

    public String Name => "CSS";

    string Minify(bool isExtended, string s, string filename)
    {
        if (isExtended)
        {
            return SimpleCSS.SimpleCSSCompiler.Compile(s, new CSSCompilerOptions() { UseVarShortcut = true });
        }

        var result = Uglify.Css(s, filename, new NUglify.Css.CssSettings()
        {
            CommentMode = NUglify.Css.CssComment.None,
            RemoveEmptyBlocks = true
        });

        if (result.HasErrors)
        {
            var er = result.Errors[0];
            throw new Exception($"{er.Message} at line {er.StartLine}, file {er.File}");
        }
        return result.Code;
    }

    public void Build(Configuration configuration)
    {
        configuration.Options.TryGetValue("extended", out object? extended);
        bool isExtended = ((JsonElement?)extended)?.GetBoolean() == true;

        string configRelativePath = Directory.GetCurrentDirectory();
        StringBuilder rawCssFiles = new StringBuilder();

        var files = configuration.GetIncludedContents(configRelativePath);

        foreach (var content in files)
        {
            string minified;
            if (content.Mode == Configuration.PathValue.File)
            {
                string fileContents = CacheIO.RetrieveFile(content.Value);
                minified = Minify(isExtended, fileContents, Path.GetFileName(content.Value));
            }
            else
            {
                string fileContents = CacheIO.RetrieveURL(content.Value);
                minified = Minify(isExtended, fileContents, content.Value);
            }
            rawCssFiles.Append(minified);
        }

        string result = rawCssFiles.ToString();
        foreach (string outputFile in configuration.GetOutputPaths(configRelativePath))
        {
            File.WriteAllText(outputFile, result);
        }
    }
}
