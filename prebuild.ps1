Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Write-Host ("Current time: " + (Get-Date -f "yyyy-MM-dd HH:mm:ss"))

    Clean

    Generate-BuildFile "all.build"

    Restore-Nuget
}

function Clean()
{
    $folders = @(dir -Recurse -Force "obj","bin","Debug","Release",".vs" -Directory)
    $folders | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }
}

function Generate-BuildFile([string] $buildfile)
{
    $files = @(dir -Recurse "*.sln" -File)

    Write-Host ("Found " + $files.Count + " solutions.")

    [string[]] $xml = @()
    $xml += '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
    $xml += '  <Target Name="Build">'

    $files | % {
        [string] $filename = $_.FullName.Substring((pwd).Path.Length+1)
        $xml += '    <MSBuild Projects="' + $filename + '" Properties="Configuration=Release" ContinueOnError="true" />'
    }

    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host ("Saving generated build file: '" + $buildfile + "'")
    Set-Content $buildfile $xml
}

function Restore-Nuget()
{
    [string] $dotnetbinary = "dotnet"

    $nugetprocesses = @()

    $packagefiles = @(dir -Recurse "packages.config" -File)
    Write-Host ("Found " + $packagefiles.Count + " package files.")
    $packagefiles | % {
        [string] $packagefile = $_.FullName
        Write-Host ("Restoring: '" + $packagefile + "'")
        $nugetprocesses += [Diagnostics.Process]::Start($dotnetbinary, ("restore " + $packagefile + " -SolutionDirectory " + (Split-Path $packagefile)))
    }

    $solutionfiles = @(dir -Recurse "*.sln" -File)
    Write-Host ("Found " + $solutionfiles.Count + " solution files.")
    $solutionfiles | % {
        [string] $solutionfile = $_.FullName
        Write-Host ("Restoring: '" + $solutionfile + "'")
        $nugetprocesses += [Diagnostics.Process]::Start($dotnetbinary, ("restore " + $solutionfile))
    }

    $nugetprocesses | % { $_.WaitForExit() }
}

Main
