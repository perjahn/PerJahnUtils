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
    [string] $md5hash = "A0AED97A5C41373C4F77E17305D135A2"
    [long] $filelength = 683824

    if (!(Test-Path $devenvexe))
    {
        return $false
    }

    if (((Get-Command | ? { $_.Name -eq "Get-FileHash" }) -and ((Get-FileHash $devenvexe -Algorithm MD5).Hash -eq $md5hash)) -or
        ((dir $devenvexe).Length -eq $filelength))
    {
        Log ("VS2015 update 3 already installed.") Green
        return $true
    }

    Log ("Unknown file hash, reinstalling: " + $devenvexe) Green
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
        Log ("Deleting folder: '" + $extractfolder + "'")
        rd -Recurse -Force $extractfolder
    }
    if (Test-Path $extractfolder)
    {
        throw ("Couldn't delete folder: '" + $extractfolder + "'")
    }
    Log ("Creating folder: '" + $extractfolder + "'")
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
    Log ("Using temp folder: '" + $tempfolder + "'")
    if (!(Test-Path $tempfolder))
    {
        Log ("Creating folder: '" + $tempfolder + "'")
        md $tempfolder | Out-Null
    }

    [long] $filesize = 7758430208


    $drive = [System.IO.DriveInfo]::GetDrives() | ? { $tempfolder.StartsWith($_.Name) }

    Log ("Disk space free on " + $drive.Name + " " + ("{0:0.00}" -f ($drive.TotalFreeSpace/1gb)) + " gb.")

    if ($drive.TotalFreeSpace -lt 40gb)
    {
        throw ([System.Net.Dns]::GetHostName() + ": Too little disk space available on " + $drive.Name + " to perform installation of VS.")
    }


    [int] $index = 0
    do
    {
        [string] $sourceurl = $sourceurls[$index]
        [string] $isofile = Join-Path $tempfolder "vs2015.3.ent_enu.iso"

        if (!(Test-Path $isofile) -or (dir $isofile).Length -ne $filesize)
        {
            Log ("Downloading: '" + $sourceurl + "' -> '" + $isofile + "'")
            $webclient = New-Object Net.WebClient

            [Diagnostics.Stopwatch] $watch = [Diagnostics.Stopwatch]::StartNew()

            try
            {
                $webclient.DownloadFile($sourceurl, $isofile)
            }
            catch
            {
                Log ("Couldn't download: '" + $sourceurl + "': " + $_.Exception.Message)
            }

            $watch.Stop()
            Log ("Download time: " + $watch.Elapsed + ", speed: " + [int]($filesize/$watch.Elapsed.TotalSeconds/1024) + " kb/s.")
        }
        else
        {
            Log ("Using local iso file: '" + $isofile + "'")
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
        Log ("Couldn't find downloaded file: '" + $isofile + "'") Red
        return
    }

    Set-Alias zip $zipexe

    zip x ("-o" + $extractfolder) -y $isofile
    if (!$?)
    {
        Log ("Couldn't extract: '" + $isofile + "' -> '" + $extractfolder + "'")
        return
    }


    [string] $installexe = Join-Path $extractfolder "vs_enterprise.exe"
    if (!(Test-Path $installexe))
    {
        Log ("Couldn't find installation program: '" + $installexe + "'") Red
        return
    }


    pushd
    cd (Split-Path $installexe)

    [Diagnostics.Stopwatch] $watch = [Diagnostics.Stopwatch]::StartNew()
    Log ("Installing VS with product key...")
    &(".\" + (Split-Path -Leaf $installexe)) "/Full" "/Q" "/ProductKey" $serial

    popd
    Wait-Process ([IO.Path]::GetFileNameWithoutExtension($installexe))
    $watch.Stop()

    Log ("Installation time: " + $watch.Elapsed)

    Log ("Done!") Green

    Log ("Deleting folder: '" + $extractfolder + "'")
    rd -Recurse -Force $extractfolder
}

function Log([string] $message, $color)
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
