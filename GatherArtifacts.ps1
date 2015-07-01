# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    # Restructure output
    cd Artifacts

    Write-Host ("Current directory: '" + (pwd).Path + "'")
    
    $folders = dir

    Write-Host ("Creating directory: 'PerJahnUtils'")
    #md PerJahnUtils
    New-Item 'PerJahnUtils' -Type Directory | Out-Null

    $folders | % {
        [string] $source = $_.Name + "\*"
        [string] $target = "PerJahnUtils"
        Write-Host ("Moving files: '" + $source + "' -> '" + $target + "'")
        move $source $target

        Write-Host ("Deleting directory: '" + $_.Name + "'")
        rd $_.Name
    }

    LogSection "Removing junk files" {
        dir PerJahnUtils -i *.pdb,*.config | {
            [string] $filename = $_.FullName
            Write-Host ("Removing file: '" + $filename + "'")
            del $filename
        }
    }

    LogSection "Gathered files" {
        dir PerJahnUtils | % { $_.FullName }
    }

    cd ..
}

function LogSection([string] $message, [ScriptBlock] $sb)
{
    if ([System.Environment]::UserInteractive -and !$env:TEAMCITY_PROJECT_NAME)
    {
        Write-Host ("*** " + $message + "...") -f Cyan
        &$sb
    }
    else
    {
        Write-Host ("##teamcity[blockOpened name='" + $message + "']")
        &$sb
        Write-Host ("##teamcity[blockClosed name='" + $message + "']")
    }
}

Main
