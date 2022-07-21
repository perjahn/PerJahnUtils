#!/usr/bin/env pwsh
Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

# Build C/C++ applications

function Main() {
    Setup-Environment
    Build
}

function Setup-Environment() {
    apt-get update
    apt-get -y install clang
}

function Build() {
    [string[]] $sourcefiles = @()

    [string[]] $cfiles = @(dir -r -file "*.c" | % { $_.FullName })
    Write-Host "Found $($cfiles.Count) C files." -f Green
    $sourcefiles += $cfiles

    [string[]] $cppfiles = @(dir -r -file "*.cpp" | % { $_.FullName } | ? { !(grep windows $_) })
    Write-Host "Found $($cppfiles.Count) C++ files." -f Green
    $sourcefiles += $cppfiles

    $sourcefiles | % {
        [string] $sourcefile = $_
        [string] $folder = Split-Path $sourcefile
        pushd $folder
        [string] $infile = Split-Path -Leaf $sourcefile
        [string] $outfile = [IO.Path]::GetFileNameWithoutExtension($sourcefile)
        if ([IO.Path]::GetExtension($infile) -eq ".c") {
            Write-Host "Compiling: clang $infile -o $outfile" -f Green
            clang $infile -o $outfile
        }
        if ([IO.Path]::GetExtension($infile) -eq ".cpp") {
            Write-Host "Compiling: clang++ $infile -o $outfile" -f Green
            clang++ $infile -o $outfile
        }
        popd
    }
}

Main
