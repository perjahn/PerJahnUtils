Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    [int] $msifileSize = 400mb

    [string] $downloadUrl = Get-DownloadUrl

    [string] $msifile = Split-Path -Leaf $downloadUrl
    if ((Test-Path $msifile) -and (dir $msifile).Length -ge $msifileSize)
    {
        Log ("Octopus already updated with: '" + $msifile + "'")
        exit 0
    }

    Robust-Download $downloadUrl $msifile $msifileSize

    Install-Octopus $msifile

    Delete-OldFiles "msi" 3
}

function Robust-Download([string] $url, [string] $outfile, [int] $msifileSize)
{
    if (!$url.StartsWith("https://download.octopusdeploy.com/"))
    {
        Log ("Invalid url: '" + $url + "'") Red
        exit 1
    }

    for ([int] $tries = 1; !(Test-Path $outfile) -or (dir $outfile).Length -lt $msifileSize; $tries++)
    {
        if (Test-Path $outfile)
        {
            Log ("Deleting (try " + $tries + "): '" + $outfile + "'")
            del $outfile
        }

        Log ("Downloading (try " + $tries + "): '" + $url + "' -> '" + $outfile + "'") Cyan
        try
        {
            Invoke-WebRequest $url -OutFile $outfile -UseBasicParsing
        }
        catch
        {
            Log ("Couldn't download (try " + $tries + "): '" + $url + "' -> '" + $outfile + "'") Yellow
            Start-Sleep 5
        }

        if(!(Test-Path $outfile) -or (dir $outfile).Length -lt $msifileSize)
        {
            if ($tries -lt 10)
            {
                Log ("Couldn't download (try " + $tries + "): '" + $url + "' -> '" + $outfile + "'") Yellow
            }
            else
            {
                Log ("Couldn't download (try " + $tries + "): '" + $url + "' -> '" + $outfile + "'") Red
                exit 1
            }
        }
    }

    Log ("Downloaded: '" + $outfile + "'")
}

function Get-DownloadUrl()
{
    [string] $pageUrl =  "https://octopus.com/downloads"

    Log ("Downloading url: '" + $pageurl + "'") Cyan

    [string] $page = Invoke-WebRequest $pageurl -UseBasicParsing
    [string[]] $rows = @($page.Split("`n"))
    Log ("Got " + $rows.Count + " rows.")

    for ([int] $i=0; $i -lt $rows.Count; $i++)
    {
        [string] $row = $rows[$i]
        if ($row.Contains("fast lane"))
        {
            for ([int] $j=$i-1; $j -ge 0; $j--)
            {
                if ($rows[$j].Contains("<a href="))
                {
                    [int] $firstQuote = $rows[$j].IndexOf("`"")
                    [int] $secondQuote = $rows[$j].IndexOf("`"", $firstQuote+1)

                    [string] $url = $rows[$j].Substring($firstQuote+1, $secondQuote-$firstQuote-1)
                    Log ("Got url: '" + $url + "'")

                    [int] $lastSlash = $url.LastIndexOf("/")
                    [string] $version = $url.Substring($lastSlash+1)

                    [string] $downloadLink = "https://download.octopusdeploy.com/octopus/Octopus." + $version + "-x64.msi"
                    Log ("Got download link: '" + $downloadLink + "'")

                    return $downloadLink
                }
            }
        }
    }
}

function Install-Octopus([string] $msifile)
{
    [string] $serviceName = "OctopusDeploy"

    Stop-Service $serviceName

    $exefile = "msiexec.exe"
    $args = "/i " + $msifile + " /quiet"

    Log ("Running: '" + $exefile + "' '" + $args + "'")

    $watch = [Diagnostics.Stopwatch]::StartNew()
    [Diagnostics.Process] $process = [Diagnostics.Process]::Start($exefile, $args)
    $process.WaitForExit()

    Log ("Waited: " + $watch.Elapsed)

    Start-Service $serviceName
}

function Delete-OldFiles([string] $extension, [int] $keep)
{
    $files = @(dir ("*." + $extension) | sort "LastWriteTime" -Descending | select -Skip $keep)

    Log ("Found " + $files.Count + " old " + $extension + " files.")
    foreach ($file in $files)
    {
        [string] $filename = $file.FullName
        Log ("Deleting: '" + $filename + "'")
        del $filename
    }
}

function Log([string] $message, $color)
{
    [string] $logfile = "UpdateOctopus.log"
    [string] $annotatedMessage = [DateTime]::UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + $message
    Add-Content $logfile $annotatedMessage

    if ($color)
    {
        Write-Host $message -f $color
    }
    else
    {
        Write-Host $message -f Green
    }
}

Main
