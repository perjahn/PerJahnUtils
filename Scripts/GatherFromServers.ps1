# edit with: powershell_ise.exe

# Retrieves files from remote servers

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main($mainargs)
{
    try
    {
        GatherFiles
    }
    catch
    {
        Log ($_.Exception.ToString())
        throw
    }
}

function GatherFiles()
{
    if (!$mainargs -or $mainargs.Count -ne 2)
    {
        Log ("Usage: powershell .\GatherFromServers.ps1 <remote path> <local folder>

Optional environment variables used:
GatherServers
GatherUsername
GatherEncrypedPassword
GatherShortServerPrefixFrom
GatherShortServerPrefixTo
GatherTestConnectivity
GatherDeleteRemoteFiles
GatherLogfile") Red
        exit 1
    }

    [string] $remotepath = $mainargs[0]
    [string] $localfolder = $mainargs[1]

    Log ("Current dir: '" + [IO.Directory]::GetCurrentDirectory() + "'")


    $cred = GetCred
    Log ("Username: '" + $cred.userName + "'")


    $servers = @(GetServers $cred)

    LogSection ("Got " + $servers.Count + " servers") {
        $servers | sort Name | select Name,LocalServerName | ft -a
    }


    TestConnectivity ($servers | % { $_.Name }) $cred


    Log ("Searching for remote files...")

    $files =
    @(Invoke-Command -ConnectionUri ($servers | % { $_.Name }) -cred $cred -args $remotepath {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

        [string] $remotepath = $args[0]
        if (Test-Path $remotepath)
        {
            dir $remotepath | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) } | % {
                $f = $_
                $f | Add-Member NoteProperty Server ([System.Net.Dns]::GetHostName())
                $f | Add-Member NoteProperty Megabytes ([long]($_.Length/1024/1024))
                $f
            }
        }
    })

    LogSection ("Found " + $files.Count + " files") {
        $files | sort Server,Name | select Server,Name,Megabytes | ft -a
    }


    [long] $blocksize = 7000000

    $files | % {
        [string] $filename = $_.Server.ToLower() + "." + $_.Name
        if ($env:GatherShortServerPrefixFrom -and $env:GatherShortServerPrefixTo)
        {
            [string] $from = $env:GatherShortServerPrefixFrom
            [string] $to = $env:GatherShortServerPrefixTo

            if ($filename.StartsWith($from))
            {
                [string] $filename = $to + $filename.Substring(($from).Length)
            }
        }

        $f = $_
        $f | Add-Member NoteProperty ConnectionUri ($servers | ? { $_.LocalServerName -eq $f.Server } | % { $_.Name })
        $f | Add-Member NoteProperty LocalFileName $filename
    }


    $files | sort Server,Name | % {
        [string] $ConnectionUri = $_.ConnectionUri
        [string] $server = $_.Server
        [string] $remotefile = $_.FullName
        [string] $localfile = Join-Path $localfolder $_.LocalFileName
        [long] $size = $_.Length

        try
        {
            GatherFile $ConnectionUri $server $remotefile $localfile $size

            if ($env:GatherDeleteRemoteFiles)
            {
                DeleteFile $ConnectionUri $server $remotefile
            }
        }
        catch
        {
            Log ($_.Exception.ToString()) Red
        }
    }
}

function GatherFile([string] $ConnectionUri, [string] $server, [string] $remotefile, [string] $localfile, [long] $size)
{
    [string] $tmpfile = $localfile + ".tmp"
    [long] $offset = 0

    if (Test-Path $localfile)
    {
        [long] $localsize = (dir $localfile).Length
        if ($localsize -eq $size)
        {
            Log ("Local file match in size, ignoring: '" + $ConnectionUri + "', '" + $server + "', '" + $remotefile + "', size: " + $size + " -> '" + $localfile + "'")
            return
        }
        else
        {
            Log ("Deleting local file: '" + $localfile + "', local file size: " + $localsize)
            del $localfile
        }
    }

    if (Test-Path $tmpfile)
    {
        [long] $tmpsize = (dir $tmpfile).Length
        if ($tmpsize -gt $size)
        {
            Log ("Deleting local temp file (because it's bigger than remote file): '" + $tmpfile + "', tmp file size: " + $tmpsize)
            del $tmpfile
        }
        else
        {
            [long] $offset = $tmpsize
            Log ("Resuming partial local file from: " + $offset)
        }
    }


    Log ("Retrieving '" + $ConnectionUri + "', '" + $server + "', '" + $remotefile + "', size: " + $size + " -> '" + $tmpfile + "'")

    for (; $offset -lt $size; $offset+=$blocksize)
    {
        if ($offset + $blocksize -gt $size)
        {
            [long] $bufsize = $size - $offset
        }
        else
        {
            [long] $bufsize = $blocksize
        }

        Log ("Offset: " + $offset + ", bufsize: " + $bufsize)

        $encodedContent =
        Invoke-Command -ConnectionUri $ConnectionUri -cred $cred -args $remotefile,$offset,$bufsize {
            Set-StrictMode -v latest
            $ErrorActionPreference = "Stop"

            [string] $remotefile = $args[0]
            [long] $offset = $args[1]
            [long] $bufsize = $args[2]

            [byte[]] $data = New-Object byte[] $bufsize
            $fs = New-Object IO.FileStream $remotefile, 'Open'
            try
            {
                $fs.Position = $offset
                [long] $readBytes = $fs.Read($data, 0, $bufsize)
            }
            finally
            {
                $fs.Close()
            }

            [string] $encodedContent = [Convert]::ToBase64String($data, 0, $bufsize)
            return $encodedContent
        }

        [byte[]] $data = [Convert]::FromBase64String($encodedContent)

        $fs = New-Object IO.FileStream $tmpfile,'Append','Write'
        try
        {
            $fs.Write($data, 0, $data.Length)
        }
        finally
        {
            $fs.Close()
        }
    }

    Log ("Renaming '" + $tmpfile + "' -> '" + (Split-Path -Leaf $localfile) + "'")
    ren $tmpfile (Split-Path -Leaf $localfile)

    Log ("Downloaded '" + $remotefile + "'")
}

function DeleteFile([string] $ConnectionUri, [string] $server, [string] $remotefile)
{
    Log ("Trying to delete remote file: '" + $server + "', '" + $remotefile + "'")

    Invoke-Command -ConnectionUri $ConnectionUri -cred $cred -args $remotefile {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

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

        [string] $remotefile = $args[0]

        if (Test-Path $remotefile)
        {
            Log ("Deleting remote file: '" + $remotefile + "'")
            dir $remotefile | del
        }
        else
        {
            Log ("Remote file not found: '" + $remotefile + "'")
        }
    }
}

function GetCred()
{
    [string] $lib = "..\Tools\Launch.dll"

    if ((!$env:GatherUsername -or !$env:GatherEncrypedPassword) -and (!(Test-Path $lib)))
    {
        $cred = Get-Credential
    }
    else
    {
        if ($env:GatherUsername)
        {
            [string] $username = $env:GatherUsername
        }
        else
        {
            Import-Module $lib
            [string] $username = Get-LaunchUsername | select -First 1
        }

        if ($env:GatherEncrypedPassword)
        {
            [string] $encrypedPassword = $env:GatherEncrypedPassword
        }
        else
        {
            Import-Module $lib
            [string] $encrypedPassword = Get-LaunchPassword | select -First 1
        }

        $ss = $encrypedPassword | ConvertTo-SecureString
        $cred = New-Object System.Management.Automation.PSCredential -argumentlist $username, $ss
    }

    return $cred
}

function GetServers($cred)
{
    [string] $lib = "..\Tools\Launch.dll"

    if ($env:GatherServers)
    {
        $servers = $env:GatherServers -split "," | % {
            [string] $ConnectionUri = $_
            if ($ConnectionUri.Contains(";"))
            {
                [string] $ConnectionUri = $ConnectionUri.Split(",")[0]
                [string] $LocalServerName = $ConnectionUri.Split(",")[1]
            }
            else
            {
                Log ("Retrieving local server name for ConnectionUri: '" + $ConnectionUri + "'")
                [string] $LocalServerName = Invoke-Command -ConnectionUri $ConnectionUri -cred $cred { return [System.Net.Dns]::GetHostName() }
            }

            $server = New-Object PSObject
            $server | Add-Member NoteProperty Name $ConnectionUri
            $server | Add-Member NoteProperty LocalServerName $LocalServerName
            $server
        }
    }
    elseif (Test-Path $lib)
    {
        Log ("Using stored hostname/server names.")

        Import-Module $lib
        $servers = @(Get-LaunchServer)
    }
    else
    {
        Log ("Please specify remote servers (set servers=hostname1,hostname2)") Red
        exit 1
    }

    return $servers
}

function TestConnectivity([string[]] $servers, $cred)
{
    if ($env:GatherTestConnectivity -and $env:GatherTestConnectivity -eq "False")
    {
        return
    }

    LogSection "Testing connectivity" {
        try
        {
            Invoke-Command -ConnectionUri $servers -cred $cred { hostname }
        }
        catch
        {
            $servers | % {
                [string] $server = $_
                Log ("Connecting to: '" + $server + "'")
                Invoke-Command -ConnectionUri $server -cred $cred { hostname }
            }
        }
    }
}

function Log([string] $message, $color)
{
    if ($env:GatherLogfile)
    {
        ("" + (Get-Date) + ": " + $message) | Out-File $env:GatherLogfile -Append
    }

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
    if ($env:GatherLogfile)
    {
        ("" + (Get-Date) + ": " + $message) | Out-File $env:GatherLogfile -Append
    }

    Write-Host ("##teamcity[blockOpened name='" + [System.Net.Dns]::GetHostName() + ": " + $message + "']") -f Cyan
    &$sb
    Write-Host ("##teamcity[blockClosed name='" + [System.Net.Dns]::GetHostName() + ": " + $message + "']") -f Cyan
}

Main $args
