# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    [Diagnostics.Stopwatch] $watch = [Diagnostics.Stopwatch]::StartNew()

    [string] $msbuild = Locate-MsBuildBinary

    .\prebuild.ps1

    &$msbuild "all.build" "/p:Configuration=Release"

    .\postbuild.ps1

    Write-Host ("Done: " + $watch.Elapsed)
}

function Locate-MsBuildBinary()
{
    if (Get-Command "msbuild" -ErrorAction SilentlyContinue)
    {
        return "msbuild"
    }
    else
    {
        [string] $vspath = "C:\Program Files (x86)\Microsoft Visual Studio"
        Write-Host ("Searching for msbuild.exe...")
        $binaries = @(dir -Recurse $vspath -Include "msbuild.exe" | ? { (Split-Path (Split-Path $_.FullName) -Leaf) -eq "amd64" })
        if ($binaries.Count -ge 1)
        {
            [string] $binary = $binaries | select -First 1 | % { $_.FullName }
            Write-Host ("Using: '" + $binary + "'")
            return $binary
        }
        else
        {
            Write-Host ("Couldn't find msbuild.exe") -f Red
            exit 1
        }
    }
}

Main
