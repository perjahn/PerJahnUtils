# edit with: powershell_ise.exe
# run with: powershell -ExecutionPolicy RemoteSigned -File InstallVS.ps1

# Must be executed with Administrator rights!

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

[string] $zipexe = Join-Path $env:programfiles "7-Zip\7z.exe"

function Main()
{
    [string] $internalurl = $env:VS2015internalurl
    [string] $serial = $env:VS2015serial


    if (!$serial)
    {
        throw ("No serial number specified, set environment variable VS2015serial.")
    }

    if (IsVSInstalled)
    {
        return
    }

    CheckZip

    [string] $extractfolder = "C:\VS2015"

    PrepareFolder $extractfolder

    [string] $isofile = DownloadIsoFile $internalurl

    InstallIsoFile $isofile $extractfolder $serial
}

function IsVSInstalled()
{
    [string] $devenvexe = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe"
    if (!(Test-Path $devenvexe))
    {
        return $false
    }
    if ((Get-FileHash $devenvexe -Algorithm MD5).Hash -eq "A0AED97A5C41373C4F77E17305D135A2")
    {
        LogMessage ("VS2015 update 3 already installed.") Green
        return $true
    }

    LogMessage ("Unknown file hash, reinstalling: " + $devenvexe) Green
    return $false
}

function CheckZip()
{
    if (!(Test-Path $zipexe))
    {
        throw ("Couldn't find 7-zip: '" + $zipexe + "'")
    }
}

function PrepareFolder([string] $extractfolder)
{
    if (Test-Path $extractfolder)
    {
        LogMessage ("Deleting folder: '" + $extractfolder + "'")
        rd -Recurse -Force $extractfolder
    }
    if (Test-Path $extractfolder)
    {
        throw ("Couldn't delete folder: '" + $extractfolder + "'")
    }
    LogMessage ("Creating folder: '" + $extractfolder + "'")
    md $extractfolder | Out-Null
}

function DownloadIsoFile([string] $internalurl)
{
    if ($internalurl)
    {
        [string[]] $sourceurls = $internalurl
    }
    else
    {
        [string[]] $sourceurls = @()
    }

    $sourceurls += "http://download.microsoft.com/download/8/4/3/843ec655-1b67-46c3-a7a4-10a1159cfa84/vs2015.3.ent_enu.iso"


    [string] $tempfolder = Join-Path $env:userprofile "Downloads"
    LogMessage ("Using temp folder: '" + $tempfolder + "'")
    if (!(Test-Path $tempfolder))
    {
        LogMessage ("Creating folder: '" + $tempfolder + "'")
        md $tempfolder | Out-Null
    }

    [long] $filesize = 7758430208


    $drive = [System.IO.DriveInfo]::GetDrives() | ? { $tempfolder.StartsWith($_.Name) }

    LogMessage ("Disk space free on " + $drive.Name + " " + ("{0:0.00}" -f ($drive.TotalFreeSpace/1gb)) + " gb.")

    if ($drive.TotalFreeSpace -lt 40gb)
    {
        throw ([System.Net.Dns]::GetHostName() + ": Too little disk space available on " + $drive.Name + " to perform installation of VS.")
    }


    [int] $index = 0
    do
    {
        [string] $sourceurl = $sourceurls[$index]
        [string] $isofile = Join-Path $tempfolder (Split-Path -Leaf $sourceurl)

        if (!(Test-Path $isofile) -or (dir $isofile).Length -ne $filesize)
        {
            LogMessage ("Downloading: '" + $sourceurl + "' -> '" + $isofile + "'")
            $webclient = New-Object Net.WebClient

            [DateTime] $t1 = Get-Date

            try
            {
                $webclient.DownloadFile($sourceurl, $isofile)
            }
            catch
            {
                LogMessage ("Couldn't download: '" + $sourceurl + "': " + $_.Exception.Message)
            }

            [DateTime] $t2 = Get-Date
            LogMessage ("Download time: " + ($t2-$t1) + ", speed: " + (($t2-$t1).TotalSeconds/$filesize/1024) + " kb/s.")
        }
        else
        {
            LogMessage ("Using local iso file: '" + $isofile + "'")
        }

        $index++
    }
    while (!(Test-Path $isofile) -or (dir $isofile).Length -ne $filesize)


    return $isofile
}

function InstallIsoFile([string] $isofile, [string] $extractfolder, [string] $serial)
{
    if (!(Test-Path $isofile))
    {
        LogMessage ("Couldn't find downloaded file: '" + $isofile + "'") Red
        return
    }

    Set-Alias zip $zipexe

    zip x ("-o" + $extractfolder) -y $isofile
    if (!$?)
    {
        LogMessage ("Couldn't extract: '" + $isofile + "' -> '" + $extractfolder + "'")
        return
    }


    [string] $installexe = "C:\VS2015\vs_enterprise.exe"
    if (!(Test-Path $installexe))
    {
        LogMessage ("Couldn't find installation program: '" + $installexe + "'") Red
        return
    }


    pushd
    cd (Split-Path $installexe)

    [DateTime] $t1 = Get-Date
    LogMessage ("Installing VS with product key...")
    &(".\" + (Split-Path -Leaf $installexe)) "/Full" "/Q" "/ProductKey" $serial

    popd
    Wait-Process ([IO.Path]::GetFileNameWithoutExtension($installexe))
    [DateTime] $t2 = Get-Date

    LogMessage ("Installation time: " + ($t2-$t1))

    LogMessage ("Done!") Green

    LogMessage ("Deleting folder: '" + $extractfolder + "'")
    rd -Recurse -Force $extractfolder
}

function LogMessage([string] $message, $color)
{
    if ($color)
    {
        Write-Host ([System.Net.Dns]::GetHostName() + ": " + $message) -f $color
    }
    else
    {
        Write-Host ([System.Net.Dns]::GetHostName() + ": " + $message)
    }
}

Main
