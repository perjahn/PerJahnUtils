# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Clean

    Generate-BuildFile "all.build"

    Download-Nuget

    Remove-SpammyBuildFile
}

function Clean()
{
    $files = @(dir -r -i obj,bin,Debug,Release)
    $files | ? { $_.Attributes -band [IO.FileAttributes]::Directory } | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }
}

function Generate-BuildFile([string] $buildfile)
{
    $files = @(dir -r -i *.sln | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) })

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
    sc $buildfile $xml
}

function Download-Nuget()
{
    curl -UseBasicParsing https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe

    $packagefiles = @(dir -Recurse packages.config)

    Write-Host ("Found " + $packagefiles.Count + " package files.")

    $packagefiles | % {
        [string] $packagefile = $_.FullName
        Write-Host ("Restoring: '" + $packagefile + "'")
        .\nuget.exe restore $packagefile -SolutionDirectory (Split-Path $packagefile)
    }
}

function Remove-SpammyBuildFile()
{
    [string] $spammybuildfile = "C:\Program Files (x86)\MSBuild\14.0\Microsoft.Common.targets\ImportAfter\Xamarin.Common.targets"
    if (Test-Path $spammybuildfile)
    {
        del $spammybuildfile -ErrorAction SilentlyContinue
    }
}

Main
