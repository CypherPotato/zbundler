$version = "v.0.4"

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

    # build
    & dotnet publish "$dir/zbundler.csproj" --nologo -v quiet -r $arch -c Release `
        -o ""$dir/bin/dist/$nameTag/"" --self-contained false /p:DebugType=None /p:DebugSymbols=false
    
    # cleanup sass libs
    $extFolders = Get-ChildItem -Path "$dir/bin/dist/$nameTag/ext/sass/" -Directory
    foreach ($folder in $extFolders) {
        if ($folder.Name -ne $arch) {
            Remove-Item -Path $folder.FullName -Recurse -Force
        }
    }

    # zip 
    & 7z a "$dir/bin/dist/$nameTag.zip" "$dir/bin/dist/$nameTag/" | Select-String "Error" -Context 10
}

Write-Host "Building done!"