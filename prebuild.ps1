#!/usr/bin/env pwsh
Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    Write-Host "Current time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")"

    Clean

    Generate-BuildFile "all.build"
}

function Clean() {
    [string[]] $filter = "obj", "bin", "Debug", "Release", ".vs", ".vscode", "x64", "packages"

    [string[]] $folders = @(dir -Recurse -Force $filter -Directory | % { $_.FullName.Substring((pwd).Path.Length + 1) })
    $folders | % {
        Write-Host "Deleting folder: '$_'"
        rd -Recurse -Force $_ -ErrorAction SilentlyContinue
    }

    [string[]] $folders = @(dir -Recurse -Force $filter -Directory | % { $_.FullName.Substring((pwd).Path.Length + 1) })
    $folders | % {
        if (Test-Path $_) {
            Write-Host "Deleting folder: '$_'" -f Yellow
            rd -Recurse -Force $_
        }
    }
}

function Generate-BuildFile([string] $buildfile) {
    [string[]] $slnfiles = @(dir -Recurse "*.sln" -File | % { $_.FullName })

    if (Test-Path "C:\Windows") {
        [string[]] $slnfiles = @($slnfiles | ? { Test-Path ([IO.Path]::ChangeExtension($_, ".vcxproj")) })
    }
    else {
        [string[]] $slnfiles = @($slnfiles | ? {
            [string] $filename = [IO.Path]::ChangeExtension($_, ".vcxproj")
            if (Test-Path $filename) {
                return $false
            }

            [string] $filename = [IO.Path]::ChangeExtension($_, ".csproj")
            if (!(Test-Path $filename)) {
                Write-Host "Excluding project: Missing csproj for solution: '$_'" -f Yellow
                return $false
            }

            [string] $content = [IO.File]::ReadAllText($filename)
            if (!$content.Contains("Microsoft.NET.Sdk")) {
                Write-Host "Excluding old C# project: '$($filename.Substring((pwd).Path.Length + 1))'" -f Yellow
                return $false
            }

            [string] $qq = "<AnalysisMode>All</AnalysisMode><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild><AnalysisLevelStyle>preview</AnalysisLevelStyle><NoWarn>CA1033,CA1063,CA1303,CA1304,CA1305,CA1310,CA1311,CA1515,CS1591,CA1816,CA1822,CA1849,CA1852,CA2007,CA2100,IDE0006,IDE0008,IDE0040,IDE0210</NoWarn><GenerateDocumentationFile>true</GenerateDocumentationFile>"

            sed -i "s;<TargetFramework>net8\.0</TargetFramework>;<TargetFramework>net9.0</TargetFramework>$($qq);g" $filename
            return $true
        })
    }

    [string[]] $slnfiles = @($slnfiles | % { $_.Substring((pwd).Path.Length + 1) })

    Write-Host "Found $($slnfiles.Count) solutions."

    [string[]] $xml = @()
    $xml += '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
    $xml += '  <Target Name="Build">'
    $xml += '    <MSBuild Targets="Restore;Build;Publish" Projects="' + ($slnfiles -join ";") + '" Properties="Configuration=Release;Runtime=linux-x64" ContinueOnError="true" BuildInParallel="true" />'
    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host -n "Dotnet version: "
    dotnet --version

    Write-Host "Saving generated build file: '$buildfile'"
    Set-Content $buildfile $xml
}

Main
