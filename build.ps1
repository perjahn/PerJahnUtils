# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    .\prebuild.ps1

    msbuild "all.build" "/p:Configuration=Release"

    .\postbuild.ps1
}

Main
