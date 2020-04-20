Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main() {
    if ($env:buildconfig) {
        [string] $buildconfig = $env:buildconfig
    }
    else {
        [string] $buildconfig = "Release"
    }

    [string] $artifactfolder = "PerJahnUtils"

    Gather-Artifacts $buildconfig $artifactfolder

    Compress-Artifacts $artifactfolder

    Write-Host "Current time: $(Get-Date -f "yyyy-MM-dd HH:mm:ss")"
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

    $exefiles = @(dir -Recurse -File *.exe -Exclude *.vshost.exe, nuget.exe |
        ? { !$_.FullName.Substring((pwd).Path.Length).Contains("$([IO.Path]::DirectorySeparatorChar)obj$([IO.Path]::DirectorySeparatorChar)") })

    Write-Host "Found $($exefiles.Count) files."

    $groups = @($exefiles | Group-Object Name)

    $exefiles = @($groups | % {
            if ($_.Count -eq 1) {
                return $_ | Select-Object -First 1
            }
            else {
                $files = @($_.Group | ? { $_.FullName.Contains("$([IO.Path]::DirectorySeparatorChar)publish$([IO.Path]::DirectorySeparatorChar)") })
                if ($files.Count -ge 1) {
                    return $files | Select-Object -First 1
                }
                else {
                    return $_ | Select-Object -First 1
                }
            }
        })

    Write-Host "Moving $($exefiles.Count) files."

    foreach ($exefile in $exefiles) {
        [string] $source = $exefile.FullName
        if ($source.Contains("Log") ) {
            Write-Host "Not moving file: '$source' -> '$artifactfolder'"
        }
        else {
            Write-Host "Moving file: '$source' -> '$artifactfolder'"
            move $source $artifactfolder
        }
    }

    Write-Host "Done moving."
}

function Compress-Artifacts([string] $artifactfolder) {
    [string] $sevenzippath = "C:\Program Files\7-Zip\7z.exe"
    if (Test-Path $sevenzippath) {
        Set-Alias zip $sevenzippath
    }
    else {
        Set-Alias zip 7z
    }

    [string] $outfile = "PerJahnUtils.7z"
    if (Test-Path $outfile) {
        Write-Host "Deleting old archive file: '$outfile'"
        del $outfile
    }

    Write-Host "Compressing $(@(dir $artifactfolder).Count) exe tools to $outfile..."
    zip a -mx9 $outfile $artifactfolder
    if (!$? -or !(Test-Path $outfile)) {
        Write-Host "Couldn't compress artifacts." -f Red
        exit 7
    }

    Write-Host "Produced artifact: '$outfile'" -f Green
}

Main
