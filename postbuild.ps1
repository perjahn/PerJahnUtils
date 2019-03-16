Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    if ($env:buildconfig)
    {
        [string] $buildconfig = $env:buildconfig
    }
    else
    {
        [string] $buildconfig = "Release"
    }

    [string] $artifactfolder = "PerJahnUtils"

    Gather-Artifacts $buildconfig $artifactfolder

    Compress-Artifacts $artifactfolder

    Write-Host ("Current time: " + (Get-Date -f "yyyy-MM-dd HH:mm:ss"))
}

function Gather-Artifacts([string] $buildconfig, [string] $artifactfolder)
{
    Write-Host ("Artifact folder: '" + $artifactfolder + "'")
    Write-Host ("Build config: '" + $buildconfig + "'")

    if (Test-Path $artifactfolder)
    {
        Write-Host ("Deleting old artifact folder: '" + $artifactfolder + "'")
        rd -Recurse $artifactfolder
    }

    Write-Host ("Creating artifact folder: '" + $artifactfolder + "'")
    md $artifactfolder | Out-Null

    $exefiles = dir -r -i *.exe -Exclude *.vshost.exe,nuget.exe | ? { !$_.FullName.Contains([IO.Path]::DirectorySeparatorChar + "obj" + [IO.Path]::DirectorySeparatorChar) }
    $exefiles | % {
        [string] $source = $_.FullName
        Write-Host ("Copying file: '" + $source + "' -> '" + $artifactfolder + "'")
        copy $source $artifactfolder
    }
}

function Compress-Artifacts([string] $artifactfolder)
{
    [string] $sevenzippath = "C:\Program Files\7-Zip\7z.exe"
    if (Test-Path $sevenzippath)
    {
        Set-Alias zip $sevenzippath
    }
    else
    {
        Set-Alias zip 7z
    }

    [string] $outfile = "PerJahnUtils.zip"
    if (Test-Path $outfile)
    {
        Write-Host ("Deleting old archive file: '" + $outfile + "'")
        del $outfile
    }

    Write-Host ("Compressing " + @(dir $artifactfolder).Count + " exe tools to " + $outfile + "...")
    zip a -mx9 $outfile (Join-Path $artifactfolder *.exe)
    if (!$? -or !(Test-Path $outfile))
    {
        Write-Host ("Couldn't compress artifacts.") -f Red
        exit 1
    }

    Write-Host ("Produced artifact: " + $outfile) -f Green
}

Main
