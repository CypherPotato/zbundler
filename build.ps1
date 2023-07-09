$version = "v.0.3"

$archs = @(
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64"
)

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dir = $dir.Replace('\', '/')

foreach ($arch in $archs) {
    $nameTag = "zbundler-$version-$arch"
    Write-Host "Building $nameTag..."
    & dotnet publish "$dir/zbundler.csproj" --nologo -v quiet -r $arch -c Release `
        -o ""$dir/bin/dist/$nameTag/"" --self-contained false /p:DebugType=None /p:DebugSymbols=false
    & 7z a "$dir/bin/dist/$nameTag.zip" "$dir/bin/dist/$nameTag/"
}

Write-Host "Building done!"