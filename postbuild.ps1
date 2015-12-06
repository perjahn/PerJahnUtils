# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    if ($env:artifactfolder)
    {
        [string] $artifactfolder = $env:artifactfolder
    }
    else
    {
        [string] $artifactfolder = "_Artifacts"
    }

    if ($env:buildconfig)
    {
        [string] $buildconfig = $env:buildconfig
    }
    else
    {
        [string] $buildconfig = "Release"
    }

    GatherFiles $artifactfolder $buildconfig
}

function GatherFiles([string] $artifactfolder, [string] $buildconfig)
{
    Write-Host ("Artifact folder: '" + $artifactfolder + "'")
    Write-Host ("Build config: '" + $buildconfig + "'")

    [string] $toolpath = "GatherOutputAssemblies\bin\Release\GatherOutputAssemblies.exe"

    if (!(Test-Path $toolpath))
    {
        Write-Host ("File not found: '" + $toolpath + "'")
        exit 1
    }

    if (Test-Path $artifactfolder)
    {
        Write-Host ("Removing artifact folder: '" + $artifactfolder + "'")
        rd -Recurse $artifactfolder
    }

    Write-Host ("Creating artifact folder: '" + $artifactfolder + "'")
    md $artifactfolder | Out-Null

    dir -r -i *.sln | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) } | % {
        [string] $solutionfile = $_.FullName
        &$toolpath $solutionfile $buildconfig $artifactfolder
    }

    dir $artifactfolder -r -i *.exe -Exclude *.vshost.exe | % {
        [string] $source = $_.FullName
        [string] $target = $artifactfolder
        Write-Host ("Copying file: '" + $source + "' -> '" + $target + "'")
        copy $source $target
    }

    dir $artifactfolder | ? { $_.Attributes -band [IO.FileAttributes]::Directory } | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse $_.FullName
    }


    [string] $zipexe = "C:\Program Files\7-Zip\7z.exe"

    if (!(Test-Path $zipexe))
    {
        Write-Host ("File not found: '" + $zipexe + "'")
        return
    }

    Set-Alias zip $zipexe


    cd $artifactfolder
    md PerJahnUtils | Out-Null
    move *.exe PerJahnUtils

    Write-Host "Zipping PerJahnUtils.zip..."
    zip a PerJahnUtils.zip PerJahnUtils\*.exe
    if (!$?)
    {
        cd ..
        Write-Host ("Couldn't zip files.")
        exit 2
    }
    del PerJahnUtils\*.exe
    cd ..
}

Main
