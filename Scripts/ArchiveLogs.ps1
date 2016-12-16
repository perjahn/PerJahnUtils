# edit with: powershell_ise.exe
# run with: powershell -ExecutionPolicy RemoteSigned -File ArchiveLogs.ps1

# This is a script that archives log files, from a log folder that is
# actively used, to compressed files in an archive folder.

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    if (!$env:logfolder)
    {
        Log ("logfolder environment variable not set.") Red
        exit 1
    }
    [string] $logfolder = $env:logfolder

    if (!$env:archivefolder)
    {
        Log ("archivefolder environment variable not set.") Red
        exit 1
    }
    [string] $archivefolder = $env:archivefolder

    [int] $daysthreashold = 30
    if (!$env:daysthreashold -or ![int]::TryParse($env:daysthreashold, [ref] $daysthreashold))
    {
        Log ("daysthreashold environment variable not set to a valid integer, using 30.") Yellow
        [int] $daysthreashold = 30
    }


    [Diagnostics.StopWatch] $sw = [Diagnostics.Stopwatch]::StartNew()

    ArchvieLogs $logfolder $archivefolder $daysthreashold

    $sw.Stop()
    Log ("Total time: " + [int]$sw.Elapsed.TotalMinutes + " minutes.") Green
}

function ArchvieLogs([string] $logfolder, [string] $archivefolder, [int] $daysthreashold)
{
    Log ("Archiving " + $daysthreashold + " days old logs from '" + $logfolder + "' -> '" + $archivefolder + "'") Green

    $cred = GetCreds

    $serverobjects = @(GetServers)

    LogSection ("Servers: " + $serverobjects.Count) {
        $serverobjects | % { Log $_.LocalServerName }
    }

    [string[]] $servers = @($serverobjects | % { $_.Name })

    [string] $date = (Get-Date).ToString("yyyy_MM_dd")

    Log ("Date: " + $date)


    VerifyEnvironment $servers $cred


    $sizesBefore = RetrieveSizes $servers $cred $logfolder $archivefolder


    # Even if log files are moved from the active log folder to the archive log
    # folder at each deploy, there will eventually be log files lingering due
    # to deprecated services leaving log files in the active log folder. These
    # must therefore also be moved by this script, in some conservative way,
    # like when log files are older than 1 week in the active log folder.
    LogSection "Moving log files" {
        MoveServerLogs $servers $cred $logfolder $archivefolder $daysthreashold
    }


    # The powershell remote host service (winrm) isn't very robust, the remote
    # session can be terminated without the client throwing any exception. First
    # try in parallel, then sequentially, to not block the build agent for
    # prolonged times which can trigger its timeout (4h), but also be able to
    # catch any potential problems and make sure it's logged correctly, which
    # can only be done in a robust way if executed sequentially. Do as little as
    # possible in each remote session, i.e. all files are moved in the previous
    # call, and statistics are collected in the next.
    LogSection "Zipping logfiles" {
        [Diagnostics.StopWatch] $sw = [Diagnostics.Stopwatch]::StartNew()
        try
        {
            CompressServerLogs $servers $cred $date $archivefolder
        }
        catch
        {
            Log ("Something failed: " + $_.Exception.ToString()) Red
        }
        $sw.Stop()
        Log ("All done in " + ([int]$sw.Elapsed.TotalSeconds) + " seconds.") Green

        $serverobjects | % {
            [string] $server = $_.Name
            [Diagnostics.StopWatch] $sw = [Diagnostics.Stopwatch]::StartNew()
            for ([int] $tries=0; $tries -lt 3; $tries++)
            {
                try
                {
                    CompressServerLogs $server $cred $date $archivefolder
                }
                catch
                {
                    Log ("Retry " + ($tries+1) + " failed: " + $_.Exception.ToString()) Red
                }
            }
            $sw.Stop()
            Log ($_.LocalServerName + ": " + [int]$sw.Elapsed.TotalSeconds + " seconds.") Green
        }
    }


    $sizesAfter = RetrieveSizes $servers $cred $logfolder $archivefolder

    PrintStatistics $sizesBefore $sizesAfter
}

function GetCreds()
{
    # Encrypted password string must be created at the machine and by the
    # user which executes this script, by running the following command:
    # Read-Host -AsSecureString | ConvertFrom-SecureString

    [string] $libpath = "..\Tools\Launch.dll"
    if (Test-Path $libpath)
    {
        Import-Module $libpath

        [string] $username = Get-LaunchUsername | select -first 1
        if (!$username)
        {
            Log ("Couldn't retrieve stored username.") Red
            exit 1
        }

        [string] $encryptedPassword = Get-LaunchPassword | select -first 1
        if (!$encryptedPassword)
        {
            Log ("Couldn't retrieve stored encrypted password.") Red
            exit 1
        }

        $ss = $encryptedPassword | ConvertTo-SecureString
        $cred = New-Object System.Management.Automation.PSCredential -argumentlist $username, $ss
    }
    else
    {
        [string] $username = $env:archiveUsername
        if (!$username)
        {
            Log ("Couldn't retrieve archiveUsername environment variable.") Red
            exit 1
        }

        [string] $encryptedPassword = $env:archivePassword
        if (!$encryptedPassword)
        {
            Log ("Couldn't retrieve archivePassword environment variable.") Red
            exit 1
        }

        $ss = $encryptedPassword | ConvertTo-SecureString
        $cred = New-Object System.Management.Automation.PSCredential -argumentlist $username, $ss
    }

    if (!$cred)
    {
        Log ("Couldn't get credentials to remote servers.")
        exit 1
    }

    return $cred
}

function GetServers()
{
    [string] $libpath = "..\Tools\Launch.dll"
    if (Test-Path $libpath)
    {
        Import-Module $libpath

        $serverobjects = Get-LaunchServer
    }
    else
    {
        [string] $servers = $env:archiveServers
        if (!$servers)
        {
            Log ("Couldn't retrieve archiveServers environment variable.") Red
            exit 1
        }

        $serverobjects = $servers.Split(",") | % {
            [string] $name = $_.Split(";") | select -First 1
            [string] $localname = $_.Split(";") | select -Last 1

            $server = New-Object PSObject
            $server | Add-Member Name $name
            $server | Add-Member LocalServerName $localname
            $server
        }
    }

    if (!$serverobjects)
    {
        Log ("Couldn't get remote servers.")
        exit 1
    }

    return $serverobjects
}

function VerifyEnvironment([string[]] $servers, $cred)
{
    LogSection "Testing connectivity" {
        Invoke-Command -cred $cred -ConnectionUri $servers { hostname }
    }


    [string] $zipexepath = "C:\Program Files\7-Zip\7z.exe"

    LogSection "Verifying 7zip installation" {
        Invoke-Command -cred $cred -ConnectionUri $servers -args $zipexepath {
            if (!(Test-Path $args[0]))
            {
                Write-Host ([System.Net.Dns]::GetHostName() + ": Couldn't find 7-zip: '" + $zipexepath + "'") -f Yellow
            }
        }
    }


    LogSection "7-Zip needs more than 1gb ram" {
        Invoke-Command -cred $cred -ConnectionUri $servers {
            winrm g "winrm/config" | ? { $_.Contains("MaxMemoryPerShellMB") } | % { [System.Net.Dns]::GetHostName() + ": " + $_.Trim() }
        }
    }
}

function RetrieveSizes([string[]] $servers, $cred, [string] $logfolder, [string] $archivefolder)
{
    Log ("Retrieving sizes...")

    $sizes = @(Invoke-Command -cred $cred -ConnectionUri $servers -args $logfolder,$archivefolder {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

        [string] $logfolder = $args[0]
        [string] $archivefolder = $args[1]

        if (!(Test-Path $logfolder))
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Creating folder: '" + $logfolder + "'")
            md $logfolder | Out-Null
        }
        if (!(Test-Path $archivefolder))
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Creating folder: '" + $archivefolder + "'")
            md $archivefolder | Out-Null
        }

        $size = New-Object PSObject
        $size | Add-Member NoteProperty Server ([System.Net.Dns]::GetHostName())
        $size | Add-Member NoteProperty LogsSize ((dir $logfolder | Measure-Object Length -Sum).Sum)
        $size | Add-Member NoteProperty ArchiveSize ((dir $archivefolder | Measure-Object Length -Sum).Sum)
        $size
    } | ? { $_ })
    
    Log ("Got " + $sizes.Count + " sizes.")

    return $sizes
}

function PrintStatistics([PSObject[]] $sizesBefore, [PSObject[]] $sizesAfter)
{
    LogSection "Calculating sizes (MB)" {
        $sizes = $sizesBefore | % {
            $before = $_
            $after = $sizesAfter | ? { $_.Server -eq $before.Server }

            $size = New-Object PSObject
            $size | Add-Member NoteProperty Server $before.Server
            $size | Add-Member NoteProperty LogsBefore ([long]($before.LogsSize/1mb))
            $size | Add-Member NoteProperty LogsAfter ([long]($after.LogsSize/1mb))
            $size | Add-Member NoteProperty ArchiveBefore ([long]($before.ArchiveSize/1mb))
            $size | Add-Member NoteProperty ArchiveAfter ([long]($after.ArchiveSize/1mb))
            $size
        }

        $sizes | sort Server | ft -a

        Log ("LogsAfter:    " + ($sizes | Measure-Object LogsAfter -Sum).Sum)
        Log ("ArchiveAfter: " + ($sizes | Measure-Object ArchiveAfter -Sum).Sum)
    }
}

function MoveServerLogs([string[]] $servers, $cred, [string] $logfolder, [string] $archivefolder, [int] $daysthreashold)
{
    Invoke-Command -cred $cred -ConnectionUri $servers -args $logfolder,$archivefolder,$daysthreashold {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

        [string] $logfolder = $args[0]
        [string] $archivefolder = $args[1]
        [int] $daysthreashold = $args[2]

        function Log([string] $message, $color)
        {
            [string] $hostname = [System.Net.Dns]::GetHostName()
            if ($color)
            {
                Write-Host ($hostname + ": " + $message) -f $color
            }
            else
            {
                Write-Host ($hostname + ": " + $message)
            }
        }
        
        function GetUniqueFileName([string] $filename)
        {
            for ([int] $i=0; $i -lt 10000; $i++)
            {
                [string] $ext = [IO.Path]::GetExtension($filename)
                [string] $newname = $filename.Substring(0, $filename.Length-$ext.Length) + "." + $i + $ext
                if (!(Test-Path $newname))
                {
                    return $newname
                }
            }
            return $null
        }


        [int] $movedcount = 0
        [int] $errorcount = 0
        [long] $movedsize = 0

        [DateTime] $oldtime = (Get-Date).AddDays(-$daysthreashold)

        dir $logfolder -File | ? { $_.Refresh | Out-Null; $_.LastWriteTime -lt $oldtime } | % {
            [string] $source = $_.FullName
            [string] $target = Join-Path $archivefolder $_.Name

            if (Test-Path $target)
            {
                [string] $newname = GetUniqueFileName $target
                if ($newname)
                {
                    Log ("Target file already exists, renaming target file: '" + $target + "' -> '" + $newname + "'") Magenta
                    [string] $target = $newname
                }
                else
                {
                    Log ("Couldn't generate an unique file name, ignoring file: '" + $target + "'") Red
                    return
                }
            }
            
            [bool] $debug = $false
            if ($debug)
            {
                Log ("Moving: '" + $source + "' -> '" + $target + "' (" + $_.LastWriteTime + ") (" + $oldtime + ")") DarkYellow
            }
            try
            {
                move $source $target
                $movedcount++
                $movedsize += $_.Length
            }
            catch
            {
                Log ("Couldn't move: '" + $source + "' -> '" + $target + "'") Yellow
                $errorcount++
            }
        }


        Log ("Moved: Files: " + $movedcount + ", Errors: " + $errorcount + ", Moved size (mb): " + [long]($movedsize/1mb))
    }
}

function CompressServerLogs([string[]] $servers, $cred, [string] $date, [string] $archivefolder)
{
    Log ("Connecting to servers: " + $servers)

    Invoke-Command -cred $cred -ConnectionUri $servers -args $date,$archivefolder {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

        [Diagnostics.StopWatch] $sw = [Diagnostics.Stopwatch]::StartNew()

        [string] $date = $args[0]
        [string] $archivefolder = $args[1]


        function Log([string] $message, $color)
        {
            [string] $hostname = [System.Net.Dns]::GetHostName()
            if ($color)
            {
                Write-Host ($hostname + ": " + $message) -f $color
            }
            else
            {
                Write-Host ($hostname + ": " + $message)
            }
        }
        
        function GetUniqueFileName([string] $filename)
        {
            for ([int] $i=0; $i -lt 10000; $i++)
            {
                [string] $ext = [IO.Path]::GetExtension($filename)
                [string] $newname = $filename.Substring(0, $filename.Length-$ext.Length) + "." + $i + $ext
                if (!(Test-Path $newname))
                {
                    return $newname
                }
            }
            return $null
        }


        [string] $zipexepath = "C:\Program Files\7-Zip\7z.exe"
        if (!(Test-Path $zipexepath))
        {
            Log ("Couldn't find 7-zip: '" + $zipexepath + "'")
            return
        }

        Set-Alias zip $zipexepath

        cd $archivefolder
        
        dir -File | ? { $_.Length -eq 0 } | % {
            Log ("Deleting empty file: '" + $_.FullName + "'")
            del $_.FullName
        }

        dir -File *.tmp | % {
            Log ("Deleting tmp file: '" + $_.FullName + "'")
            del $_.FullName
        }

        $files = @(dir -File | ? { (".zip", ".7z", ".gz") -notcontains $_.Extension })

        Log ("Got " + $files.Count + " files.")

        if ($files.Count -eq 0)
        {
            return
        }

        $filegroups = @($files | group {
            if ($_.Name.IndexOf(".") -eq -1)
            {
                [string] $groupname = $_.BaseName
            }
            else
            {
                [string] $groupname = $_.Name.Substring(0, $_.Name.IndexOf("."))
            }

            return $groupname
        })

        Log ("Got " + $filegroups.Count + " file groups: '" + (($filegroups | % { $_.Name }) -join "', '") + "'")

        $filegroups | ? { $_.Name } | % {
            [string] $filepattern = $_.Name + ".*log*"

            $logfiles = @(dir $filepattern)
            if ($logfiles.Count -eq 0)
            {
                Log ("No log files found: " + $filepattern)
                return
            }

            [string] $zipfile = $_.Name + "_" + $date + ".zip"

            if (Test-Path $zipfile)
            {
                [string] $newname = GetUniqueFileName $zipfile
                if ($newname)
                {
                    Log ($_.Name + ": Zip file already exists, using another zip file name: '" + $zipfile + "' -> '" + $newname + "'") Magenta
                    [string] $zipfile = $newname
                }
                else
                {
                    Log ($_.Name + ": Couldn't generate an unique zip file name, ignoring zipping: '" + $zipfile + "'") Red
                    return
                }
            }


            [string] $ziplog = Join-Path $env:temp "ziplog.txt"

            Log ("Zipping: " + $filepattern + " (" + @(dir $filepattern).Count + " files) -> '" + $zipfile + "'")

            [int] $maxthreads = 4

            zip a -mx9 ("-mmt" + $maxthreads) $zipfile $filepattern | Out-File $ziplog -Encoding Default
            if (!$?)
            {
                Log ("Couldn't zip file: '" + $zipfile + "'") Red
                type $ziplog
            }
            else
            {
                Log ("Deleting: " + @(dir $filepattern).Count + " files.")
                del $filepattern
            }
            if (Test-Path $ziplog)
            {
                del $ziplog
            }
        }

        Log ("Done in " + ([int]$sw.Elapsed.TotalSeconds) + " seconds!") Green
    }
}

function Log([string] $message, $color)
{
    [string] $hostname = [System.Net.Dns]::GetHostName()
    if ($color)
    {
        Write-Host ($hostname + ": " + $message) -f $color
    }
    else
    {
        Write-Host ($hostname + ": " + $message)
    }
}

function LogSection([string] $message, [ScriptBlock] $sb)
{
    Write-Host ("##teamcity[blockOpened name='" + [System.Net.Dns]::GetHostName() + ": " + $message + "']") -f Cyan
    &$sb
    Write-Host ("##teamcity[blockClosed name='" + [System.Net.Dns]::GetHostName() + ": " + $message + "']") -f Cyan
}

Main
