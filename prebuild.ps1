# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Clean

    Generate-BuildFile "all.build"

    Restore-Nuget

    Remove-SpammyBuildFile
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
        [string] $filename = $_
        if ($filename.StartsWith('.\'))
        {
            $filename = $filename.Substring(2)
        }

        $xml += '    <MSBuild Projects="' + $filename + '" Properties="Configuration=Release" ContinueOnError="true" />'
    }

    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host ("Saving generated build file: '" + $buildfile + "'")
    Set-Content $buildfile $xml
}

function Restore-Nuget()
{
    [string] $nugeturl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
    [string] $nugetbinary = "nuget.exe"
    Write-Host ("Downloading: '" + $nugeturl + "' -> '" + $nugetbinary + "'")
    Invoke-WebRequest -UseBasicParsing $nugeturl -OutFile $nugetbinary

    $nugetprocesses = @()

    $packagefiles = @(dir -Recurse "packages.config" -File)
    Write-Host ("Found " + $packagefiles.Count + " package files.")
    $packagefiles | % {
        [string] $packagefile = $_.FullName
        Write-Host ("Restoring: '" + $packagefile + "'")
        $nugetprocesses += [Diagnostics.Process]::Start($nugetbinary, ("restore " + $packagefile + " -SolutionDirectory " + (Split-Path $packagefile)))
    }

    $solutionfiles = @(dir -Recurse "*.sln" -File)
    Write-Host ("Found " + $solutionfiles.Count + " solution files.")
    $solutionfiles | % {
        [string] $solutionfile = $_.FullName
        Write-Host ("Restoring: '" + $solutionfile + "'")
        $nugetprocesses += [Diagnostics.Process]::Start($nugetbinary, ("restore " + $solutionfile))
    }

    $nugetprocesses | % { $_.WaitForExit() }
}

function Remove-SpammyBuildFile()
{
    [string] $spammybuildfile = "C:\Program Files (x86)\MSBuild\14.0\Microsoft.Common.targets\ImportAfter\Xamarin.Common.targets"
    if (Test-Path $spammybuildfile)
    {
        Write-Host ("Deleting spammy build file: '" + $spammybuildfile + "'")
        del $spammybuildfile -ErrorAction SilentlyContinue
    }
}

Main
