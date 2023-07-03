using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace zbundler;

internal class Ext
{
    public static string? GetExtFile(string ext, string binary)
    {
        string runnableName;
        if (Program.CurrentOS == PlatformOS.Windows)
        {
            runnableName = Program.NormalizedCombine(
                Program.ExecutableDirectory,
                "/ext/", ext, "win"
            );
        }
        else if (Program.CurrentOS == PlatformOS.Linux)
        {
            runnableName = Program.NormalizedCombine(
                Program.ExecutableDirectory,
                "/ext/", ext, "linux"
            );
        }
        else if (Program.CurrentOS == PlatformOS.OSX)
        {
            runnableName = Program.NormalizedCombine(
                Program.ExecutableDirectory,
                "/ext/", ext, "osx"
            );
        }
        else
        {
            return null;
        }

        if (Program.CurrentArch == Architecture.Arm)
        {
            runnableName += "-arm";
        }
        else if (Program.CurrentArch == Architecture.Arm64)
        {
            runnableName += "-arm64";
        }
        else if (Program.CurrentArch == Architecture.X64)
        {
            runnableName += "-x64";
        }
        else
        {
            Build.PrintBuildError("Incompatible processor architeture.");
            return null;
        }

        runnableName = Program.NormalizedCombine(runnableName, binary);

        if (!File.Exists(runnableName))
        {
            string __filename = runnableName.Substring(Program.ExecutableDirectory.Length);
            Build.PrintBuildError($"Extension not found for {ext}: {__filename}");
            return null;
        }

        return runnableName;
    }
}



public enum PlatformOS
{
    OSX,
    Linux,
    Windows
}