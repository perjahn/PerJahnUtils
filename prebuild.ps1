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
    $folders = @(dir -Recurse -Force "obj","bin","Debug","Release",".vs","x64","packages" -Directory)
    $folders | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }

    $folders = @(dir -Recurse -Force "obj","bin","Debug","Release",".vs","x64","packages" -Directory)
    $folders | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'") -f Yellow
        rd -Recurse -Force $_.FullName
    }
}

function Generate-BuildFile([string] $buildfile)
{
    $files = @(dir -Recurse "*.sln" -File) | ? { @(Get-Content $_.FullName).Contains("# Visual Studio 15") }
    [string[]] $filenames = $files | % { $_.FullName.Substring((pwd).Path.Length+1) }

    Write-Host ("Found " + $files.Count + " solutions.")

    [string[]] $xml = @()
    $xml += '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
    $xml += '  <Target Name="Build">'
    $xml += '    <MSBuild Targets="Restore;Build;Publish" Projects="' + ($filenames -join ";") + '" Properties="Configuration=Release;Runtime=win-x64" ContinueOnError="true" BuildInParallel="true" />'
    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host ("Saving generated build file: '" + $buildfile + "'")
    Set-Content $buildfile $xml
}

function Restore-Nuget()
{
    [string] $include = "Microsoft.DotNet.ILCompiler"
    [string] $version = "1.0.0-alpha-27519-01"

    $files = @(dir -r -i "*.csproj" | ? { $_.Length -lt 1kb })
    Write-Host ("Found " + $files.Count + " cs projects.")

    foreach ($projectfile in $files)
    {
        [string] $folder = Split-Path $projectfile.FullName

        [string] $nugetfile = Join-Path $folder "nuget.config"
        [string] $nugetconfig = '<configuration><packageSources><add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" /></packageSources></configuration>'
        Write-Host ("Creating: '" + $nugetfile + "'")
        Set-Content $nugetfile $nugetconfig


        Write-Host ("Reading project: '" + $projectfile + "'")
        [xml] $xml = Get-Content $projectfile

        AddTargetRuntime $xml
        AddPackageReference $xml $include $version

        SaveXml $projectfile $xml
    }


    [string] $dotnetbinary = "dotnet"

    $restoreprocesses = @()
<#
    $packagefiles = @(dir -Recurse "packages.config" -File)
    Write-Host ("Found " + $packagefiles.Count + " package files.")
    $packagefiles | % {
        [string] $packagefile = $_.FullName
        Write-Host ("Restoring: '" + $packagefile + "'")
        $restoreprocesses += [Diagnostics.Process]::Start($dotnetbinary, ("restore " + $packagefile + " -r win-x64"))
    }

    $solutionfiles = @(dir -Recurse "*.sln" -File)
    Write-Host ("Found " + $solutionfiles.Count + " solution files.")
    $solutionfiles | % {
        [string] $solutionfile = $_.FullName
        Write-Host ("Restoring: '" + $solutionfile + "'")
        $restoreprocesses += [Diagnostics.Process]::Start($dotnetbinary, ("restore " + $solutionfile + " -r win-x64"))
    }

    $restoreprocesses | % { $_.WaitForExit() }
#>
    [Console]::ForegroundColor = [ConsoleColor]::Gray
}

function AddTargetRuntime($xml)
{
    $nodes = @($xml.DocumentElement.SelectNodes("PropertyGroup"))

    Write-Host ("Found " + $nodes.Count + " PropertyGroup.")

    foreach ($node in $nodes)
    {
        $targets = @($node.SelectNodes("TargetLatestRuntimePatch"))
        if ($targets.Count -gt 0)
        {
            foreach ($target in $targets)
            {
                Write-Host ("Updating existing TargetLatestRuntimePatch") -f Green
                $target.InnerText = "true"
            }
        }
        else
        {
            Write-Host ("Adding TargetLatestRuntimePatch") -f Green
            $target = $xml.CreateElement("TargetLatestRuntimePatch")
            $target.InnerText = "true"
            $node.AppendChild($target) | Out-Null
        }
    }
}

function AddPackageReference($xml, [string] $include, [string] $version)
{
    [string] $xpath = "ItemGroup/PackageReference[@Include='" + $include + "'][@Version='" + $version + "']"
    $nodes = $xml.DocumentElement.SelectNodes($xpath)

    if ($nodes.Count -gt 0)
    {
        Write-Host ("PackageReference already exists.")
        return
    }

    if ($xml.DocumentElement.SelectNodes("ItemGroup").Count -eq 0)
    {
        Write-Host ("Adding ItemGroup")
        $itemGroup = $xml.CreateElement("ItemGroup")
        $xml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }

    Write-Host ("Adding PackageReference: '" + $include + "', '" + $version + "'")
    $packageReference = $xml.CreateElement("PackageReference")
    $packageReference.SetAttribute("Include", $include)
    $packageReference.SetAttribute("Version", $version)
    $xml.DocumentElement.SelectNodes("ItemGroup")[0].AppendChild($packageReference) | Out-Null
}

function SaveXml([string] $filename, $xml)
{
    Write-Host ("Saving project file: '" + $filename + "'")
    [Xml.XmlWriterSettings] $settings = New-Object Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object Text.UTF8Encoding($false)
    $settings.OmitXmlDeclaration = $true
    $writer = [Xml.XmlWriter]::Create($filename, $settings)
    $xml.Save($writer)
    $writer.Dispose()
}

Main
