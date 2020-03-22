Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    [Diagnostics.Stopwatch] $watch = [Diagnostics.Stopwatch]::StartNew()

    ./prebuild.ps1

    Write-Host ("*"*100) -f Cyan

    dotnet build all.build -c Release -r win-x64

    Write-Host ("*"*100) -f Cyan

    ./postbuild.ps1

    Write-Host "Done: $($watch.Elapsed)" -f Cyan
}

Main
