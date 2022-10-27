#!/usr/bin/env pwsh
Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    if ($env:buildconfig) {
        [string] $buildconfig = $env:buildconfig
    }
    else {
        [string] $buildconfig = "Release"
    }

    [string] $artifactfolder = "perjahnutils"

    Gather-Artifacts $buildconfig $artifactfolder

    Compress-Artifacts $artifactfolder

    Write-Host "Current time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")"
}

function Gather-Artifacts([string] $buildconfig, [string] $artifactfolder) {
    Write-Host "Artifact folder: '$artifactfolder'"
    Write-Host "Build config: '$buildconfig'"

    if (Test-Path $artifactfolder) {
        Write-Host "Deleting old artifact folder: '$artifactfolder'"
        rd -Recurse $artifactfolder
    }

    Write-Host "Creating artifact folder: '$artifactfolder'"
    md $artifactfolder | Out-Null

    [string[]] $publishfolders = @(dir -Recurse -Directory publish | % { $_.FullName.Substring((pwd).Path.Length + 1) })

    Write-Host "Found $($publishfolders.Count) publish folders."

    [string[]] $binfiles = @($publishfolders | % { dir -File $_ | % { $_.FullName.Substring((pwd).Path.Length + 1) } })

    Write-Host "Found $($binfiles.Count) files."

    foreach ($source in $binfiles) {
        if ($source.Contains("Log")) {
            Write-Host "Not moving file: '$source' -> '$artifactfolder'" -f Yellow
            continue
        }
        if (Test-Path (Join-Path $artifactfolder (Split-Path -Leaf $source))) {
            Write-Host "Not moving file: '$source' -> '$artifactfolder'" -f Yellow
            continue
        }
        Write-Host "Moving file: '$source' -> '$artifactfolder'"
        move $source $artifactfolder
    }

    Gather-Native $artifactfolder

    Write-Host "Done moving."
}

function Gather-Native([string] $artifactfolder) {
    [string[]] $sourcefiles = @()

    [string[]] $cfiles += @(dir -r -file "*.c" | % { $_.FullName })
    Write-Host "Found $($cfiles.Count) C files." -f Green
    $sourcefiles += $cfiles

    [string[]] $cppfiles += @(dir -r -file "*.cpp" | % { $_.FullName } | ? { !(grep windows $_) })
    Write-Host "Found $($cppfiles.Count) C++ files." -f Green
    $sourcefiles += $cppfiles

    $sourcefiles | % {
        [string] $sourcefile = $_
        [string] $folder = Split-Path $sourcefile
        [string] $outfile = (Join-Path $folder ([IO.Path]::GetFileNameWithoutExtension($sourcefile))).Substring((pwd).Path.Length + 1)
        if (Test-Path $outfile) {
            Write-Host "Moving outfile: '$outfile' -> '$artifactfolder'" -f Green
            move $outfile $artifactfolder
        }
        else {
            Write-Host "Outfile not found: '$outfile'" -f Yellow
        }
    }
}

function Compress-Artifacts([string] $artifactfolder) {
    [string] $sevenzippath = "C:\Program Files\7-Zip\7z.exe"
    if (Test-Path $sevenzippath) {
        Set-Alias zip $sevenzippath
    }
    else {
        Set-Alias zip 7z
    }

    [string] $outfile = "perjahnutils.7z"
    if (Test-Path $outfile) {
        Write-Host "Deleting old archive file: '$outfile'"
        del $outfile
    }

    Write-Host "Compressing $(@(dir $artifactfolder).Count) bin tools to $outfile..."
    zip a -mx9 $outfile $artifactfolder
    if (!$? -or !(Test-Path $outfile)) {
        Write-Host "Couldn't compress artifacts." -f Red
        exit 7
    }

    Write-Host "Produced artifact: '$outfile'" -f Green
}

Main
