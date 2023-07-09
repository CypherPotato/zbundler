<?php

$version = "v.0.3";

$archs = [
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64"
];

$dir = __DIR__;
$dir = str_replace('\\', '/', $dir);

foreach ($archs as $arch) {
    $nameTag = "zbundler-$version-$arch";
    echo "Building $nameTag...\n";
    exec("dotnet publish \"$dir/zbundler.csproj\" --nologo -v quiet -r $arch -c Release " .
        "-o \"$dir/bin/dist/$nameTag/\" --self-contained false /p:DebugType=None /p:DebugSymbols=false");
    exec("7z a \"$dir/bin/dist/$nameTag.zip\" \"$dir/bin/dist/$nameTag/\"");
}

echo "Done!\n";