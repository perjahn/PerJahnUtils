# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    if ($env:artifactpath)
    {
        [string] $artifactpath = $env:artifactpath
    }
    else
    {
        [string] $artifactpath = "_Artifacts"
    }

    if ($env:buildconfig)
    {
        [string] $buildconfig = $env:buildconfig
    }
    else
    {
        [string] $buildconfig = "Release"
    }

    GatherFiles $artifactpath $buildconfig
}

function GatherFiles([string] $artifactpath, [string] $buildconfig)
{
    Write-Host ("Artifact path: '" + $artifactpath + "'")
    Write-Host ("Build config: '" + $buildconfig + "'")

    [string] $toolpath = "GatherOutputAssemblies\bin\Release\GatherOutputAssemblies.exe"

    if (!(Test-Path $toolpath))
    {
        Write-Host ("File not found: '" + $toolpath + "'")
        return 1
    }

    if (Test-Path $artifactpath)
    {
        write-Host ("Removing artifact directory: '" + $artifactpath + "'")
        rd -Recurse $artifactpath
    }

    write-Host ("Creating artifact directory: '" + $artifactpath + "'")
    md $artifactpath | Out-Null

    dir -r -i *.sln | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) } | % {
        [string] $solutionfile = $_
        &$toolpath $solutionfile $buildconfig $artifactpath
    }
}

Main
