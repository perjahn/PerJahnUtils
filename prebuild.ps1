#!/usr/bin/env pwsh
Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    Write-Host "Current time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")"

    Clean

    Generate-SolutionFile "perjahnutils.slnx"
}

function Clean() {
    [string[]] $filter = "obj", "bin", "Debug", "Release", ".vs", ".vscode", "x64", "packages"

    [int] $offset = (Get-Location).Path.Length + 1

    [string[]] $folders = @(dir -Recurse -Force $filter -Directory | % { $_.FullName.Substring($offset) })
    $folders | % {
        Write-Host "Deleting folder: '$_'"
        rd -Recurse -Force $_ -ErrorAction SilentlyContinue
    }

    [string[]] $folders = @(dir -Recurse -Force $filter -Directory | % { $_.FullName.Substring($offset) })
    $folders | % {
        if (Test-Path $_) {
            Write-Host "Deleting folder: '$_'" -f Yellow
            rd -Recurse -Force $_
        }
    }
}

function Generate-SolutionFile([string] $solutionfile) {
    [string[]] $projfiles = @(dir -r *.csproj | % { $_.FullName })
    Write-Host "Found $($projfiles.Count) projects." -f Green

    [string] $xmlstring = '<Solution><Configurations><Platform Name="Any CPU" /><Platform Name="x64" /><Platform Name="x86" /></Configurations>'

    [int] $offset = (Get-Location).Path.Length + 1
    $projfiles | % {
        [string] $content = Get-Content $_
        if (!$content.Contains("Microsoft.NET.Sdk")) {
            Write-Host "Excluding old C# project: '$($_.Substring($offset))'" -f Yellow
        }
        else {
            $xmlstring += '<Project Path="' + $_.Substring($offset) + '" />'

            [string] $qq = "<AnalysisMode>All</AnalysisMode><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild><AnalysisLevelStyle>preview</AnalysisLevelStyle><NoWarn>CA1031,CA1303,CA1304,CA1305,CA1307,CA1309,CA1310,CA1311,CA1515,CS1591,CA1820,CA1822,CA1849,CA1852,CA2007,CA2201,CA2234,CA2251,CA5392,IDE0008,IDE0032,IDE0040,IDE0044,IDE0210</NoWarn><GenerateDocumentationFile>true</GenerateDocumentationFile>"
            sed -i "s;<TargetFramework>net9\.0</TargetFramework>;<TargetFramework>net9.0</TargetFramework>$($qq);g" $_
        }
    }

    $xmlstring += '</Solution>'

    [xml] $xml = $xmlstring

    Write-Host "Saving generated solution file: '$solutionfile'"
    $xml.Save($solutionfile)
}

Main
