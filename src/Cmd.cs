using CommandLine;

namespace zbundler.src;

[Verb("build", false, HelpText = "Builds the distribution files to the output directory, from an configuration file.")]
class BuildCmd
{
    [Option('c', "config", Required = false, HelpText = "Specifies the path to the configuration file in the current directory. Can be an relative or absolute path.")]
    public string? InputConfigurationFile { get; set; }
}

[Verb("watch", false, HelpText = "Starts watching the input files from the configuration file and compiles as soon as there is a change in the files.")]
class WatchCmd
{
    [Option('c', "config", Required = false, HelpText = "Specifies the path to the configuration file in the current directory. Can be an relative or absolute path.")]
    public string InputConfigurationFile { get; set; } = null!;
}