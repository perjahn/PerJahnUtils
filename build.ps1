Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    [Diagnostics.Stopwatch] $watch = [Diagnostics.Stopwatch]::StartNew()

    ./prebuild.ps1

    dotnet build all.build /p:Configuration=Release

    ./postbuild.ps1

    Write-Host ("Done: " + $watch.Elapsed)
}

Main
