Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    Write-Host "Current time: $(Get-Date -f "yyyy-MM-dd HH:mm:ss")"

    Clean

    SingleExe

    Generate-BuildFile "all.build"
}

function Clean() {
    $folders = @(dir -Recurse -Force "obj", "bin", "Debug", "Release", ".vs", "x64", "packages" -Directory)
    $folders | % {
        Write-Host "Deleting folder: '$($_.FullName)'"
        rd -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }

    $folders = @(dir -Recurse -Force "obj", "bin", "Debug", "Release", ".vs", "x64", "packages" -Directory)
    $folders | % {
        Write-Host "Deleting folder: '$($_.FullName)'" -f Yellow
        rd -Recurse -Force $_.FullName
    }
}

function SingleExe() {
    $files = @(dir *.csproj -Recurse)
    Write-Host "Found $($files.Count) project files."

    foreach ($file in $files) {
        [string] $filename = $file.FullName
        Write-Host "Reading project file: '$filename'"
        [xml] $xml = Get-Content $filename

        $groups = @($xml.SelectNodes("/Project/PropertyGroup"))
        foreach ($group in $groups) {
            $newelement = $xml.CreateElement("RuntimeIdentifier")
            $newelement.InnerText = "win10-x64"
            $group.AppendChild($newelement) | Out-Null

            $newelement = $xml.CreateElement("PublishSingleFile")
            $newelement.InnerText = "true"
            $group.AppendChild($newelement) | Out-Null

            $newelement = $xml.CreateElement("PublishTrimmed")
            $newelement.InnerText = "true"
            $group.AppendChild($newelement) | Out-Null

            $newelement = $xml.CreateElement("PublishReadyToRun")
            $newelement.InnerText = "false"
            $group.AppendChild($newelement) | Out-Null

            $newelement = $xml.CreateElement("PublishReadyToRunShowWarnings")
            $newelement.InnerText = "true"
            $group.AppendChild($newelement) | Out-Null
        }

        $xml.Save($filename)
    }
}

function Generate-BuildFile([string] $buildfile) {
    $files = @(dir -Recurse "*.sln" -File | ? {
            @(Get-Content $_.FullName).Contains("# Visual Studio 15") -or
            @(Get-Content $_.FullName).Contains("# Visual Studio Version 16") })
    [string[]] $filenames = $files | % { $_.FullName.Substring((pwd).Path.Length + 1) }

    Write-Host "Found $($files.Count) solutions."

    [string[]] $xml = @()
    $xml += '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
    $xml += '  <Target Name="Build">'
    $xml += '    <MSBuild Targets="Restore;Build;Publish" Projects="' + ($filenames -join ";") + '" Properties="Configuration=Release;Runtime=win-x64" ContinueOnError="true" BuildInParallel="true" />'
    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host "Saving generated build file: '$buildfile'"
    Set-Content $buildfile $xml
}

Main
