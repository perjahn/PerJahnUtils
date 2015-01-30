# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function GetFolderContent([string[]] $brands, [long] $maxsize, [string] $sourceenv, [string] $targetenv, [string] $sourcetype, [string] $targettype)
{
    if ($brands)
    {
        $syncs = Get-LaunchSyncFolder -env $sourceenv,$targetenv -servertype $sourcetype,$targettype -brand $brands
    }
    else
    {
        $syncs = Get-LaunchSyncFolder -env $sourceenv,$targetenv -servertype $sourcetype,$targettype
    }

    if (!$syncs)
    {
        throw "No sync folders found."
    }

    $syncs | ft -a

    Write-Host ([System.Net.Dns]::GetHostName() + ": .NET Version: " + [Environment]::Version.ToString())
    Add-Type -Path (Join-Path $env:windir "Microsoft.NET\Framework64\v4.0.30319\System.IO.Compression.FileSystem.dll")


    $cred = Get-Credential $env:username@$env:userdomain

    Write-Host ([System.Net.Dns]::GetHostName() + ": Using file size threshold: " + $maxsize)

    [string[]] $remoteServers = $syncs | % { $_.Server } | select -unique
    #[string[]] $remoteServers = $syncs | % { $_.Server } | select -unique | ? { !$_.ToLower().StartsWith([System.Net.Dns]::GetHostName().ToLower() + ".") }
    #[string[]] $localServers = $syncs | % { $_.Server } | select -unique | ? { $_.ToLower().StartsWith([System.Net.Dns]::GetHostName().ToLower() + ".") }

    [Hashtable] $htargs = @{ syncs = $syncs; maxsize = $maxsize }

    [ScriptBlock] $sb = {
        Set-StrictMode -v latest
        $ErrorActionPreference = "Stop"

        Write-Host ([System.Net.Dns]::GetHostName() + ": .NET Version: " + [Environment]::Version.ToString())
        Add-Type -Path (Join-Path $env:windir "Microsoft.NET\Framework64\v4.0.30319\System.IO.Compression.FileSystem.dll")

        [Environment]::CurrentDirectory=(pwd).Path;

        #if (!$localServers)
        #{
            $syncs = $args[0].syncs
            [long] $maxsize = $args[0].maxsize
        #}

        [string] $path = $syncs | ? { $_.Server.Split(".")[0] -eq [System.Net.Dns]::GetHostName() } | % { $_.Path }

        [int] $totalfiles = 0
        [int] $totalfilesTooBig = 0
        [long] $totalsize = 0
        [string] $folder = "tree_" + [System.Net.Dns]::GetHostName().ToLower()
        [string] $filename = Join-Path $folder ("tree_" + [System.Net.Dns]::GetHostName().ToLower() + ".txt")
        [string] $zipfile = "tree_" + [System.Net.Dns]::GetHostName().ToLower() + ".zip"



        if (Test-Path $folder)
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Removing folder: '" + $folder + "'")
            rd -recurse -force $folder
        }

        Write-Host ([System.Net.Dns]::GetHostName() + ": Creating folder: '" + $folder + "'")
        md $folder | Out-Null


        if (!(Test-Path $path))
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Creating folder: '" + $path + "'")
            md $path | Out-Null
        }


        Write-Host ([System.Net.Dns]::GetHostName() + ": Recursing into: '" + $path + "'...")

        [DateTime] $t1 = Get-Date

        Write-Host ([System.Net.Dns]::GetHostName() + ": Writing to file: '" + $filename + "'")
    	dir $path -recurse | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) } | % {
            if ($_.Length -le $maxsize)
            {
                $totalfiles++
                $totalsize += $_.Length
                [string] $row = $_.LastWriteTime.ToString("yyMMdd tHHmmss") + "`t" + $_.Length + "`t" + $_.FullName.Substring($path.Length+1)
                #Write-Host (">>" + $row + "<<")
                $row
            }
            else
            {
                $totalfilesTooBig++
            }
        } | Out-File $filename UTF8

        [DateTime] $t2 = Get-Date

        Write-Host ([System.Net.Dns]::GetHostName() + ": Time: " + ($t2-$t1) + ", Files: " + $totalfiles + ", Size: " + ("{0:0.00}" -f ($totalsize/1mb)) + " mb, (Too big files: " + $totalfilesTooBig + ")")

        Write-Host ([System.Net.Dns]::GetHostName() + ": Creating zip file from folder: '" + $folder + "' -> '" + $zipfile + "'")

        if (Test-Path $zipfile)
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Deleting zip file: '" + $zipfile + "'")
            del -force $zipfile
        }

        Add-Type -Assembly System.IO.Compression.FileSystem
        $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
        [System.IO.Compression.ZipFile]::CreateFromDirectory($folder, $zipfile, $compressionLevel, $false)

        [byte[]] $data = [IO.File]::ReadAllBytes($zipfile)

        $encodedContent = [Convert]::ToBase64String($data, 0, $data.Length)
        [Hashtable] $ht = @{ encodedContent=$encodedContent; server=[System.Net.Dns]::GetHostName().ToLower() }
        return $ht
    }

    [Hashtable[]] $encodedContents = @(Invoke-Command -cred $cred -cn $remoteServers -args $htargs -scriptblock $sb)
    #Gather $remoteServers $cred.UserName.Split("@")[0]
    #Gather $remoteServers $cred

<#
    if ($localServers)
    {
        $localServers | % {
            [string] $server = $_
            Write-Host ("Gathering files on local server: '" + $server + "'")
            &$sb
        }
    }
#>

    $encodedContents | % {
        [string] $zipfile = "tree_" + $_.server + ".zip"

        Write-Host ([System.Net.Dns]::GetHostName() + ": Saving zip file: '" + $zipfile +"'")
        [byte[]] $data = [Convert]::FromBase64String($_.encodedContent)
        [IO.File]::WriteAllBytes($zipfile, $data)

        [string] $filename = [IO.Path]::ChangeExtension($zipfile, ".txt")
        if (Test-Path $filename)
        {
            Write-Host ([System.Net.Dns]::GetHostName() + ": Deleting file: '" + $filename + "'")
            del -force $filename
        }

        Write-Host ([System.Net.Dns]::GetHostName() + ": Extracting zip file: '" + $zipfile +"'")
        [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, ".")
    }

}

function Gather([string[]] $servers, [string] $username)
{
    $servers | % {
        [string] $server = $_
        [string] $sourcepath = "\\" + $server + "\c`$\Users\" + $username + "\Documents\tree*.txt"
        dir $sourcepath | % {
            Write-Host ("Copying: '" + $_ + "' -> .")
            copy $_ .
        }
    }
}

function GetIdentifiers($cred)
{
    Import-Module .\Launch.dll

    [string] $SQLServer = "sqldb.mydomain.local"

    [string[]] $brands = "Brand1","Brand2"

    [Hashtable[]] $brandsql = @($brands | % { @{ brand=$_ ; sql=GetIdentifierSQL "Staging" $_ }})

    Invoke-Command -cred $cred -cn $SQLServer -args $brandsql {
        [Hashtable[]] $brandsql = $args

        [DateTime] $t1 = Get-Date
        Add-PSSnapin SqlServerCmdletSnapin100
        [DateTime] $t2 = Get-Date
        Write-Host ([System.Net.Dns]::GetHostName() + ": Load sql snapin time: " + ($t2-$t1))

        [string[]] $sqls = $args

        $brandsql | % {
            [string] $brand = $_.brand
            [string] $sql = $_.sql
            Write-Host ([System.Net.Dns]::GetHostName() + ": Brand: '" + $brand + "'")

            [DateTime] $t1 = Get-Date
            [string[]] $identifiers = Invoke-Sqlcmd $sql | Select-Object -ExpandProperty Identifier
            [DateTime] $t2 = Get-Date

            Write-Host ([System.Net.Dns]::GetHostName() + ": Run sql execute time: " + ($t2-$t1) + ", Database identifiers: "+ $identifiers.Count)
            [string] $filename = "identifier_" + $brand.ToLower() + ".txt"
            $identifiers | Out-File $filename UTF8
        }
    }

    GatherIdentifiers $SQLServer $cred.UserName.Split("@")[0]
}

function GetIdentifierSQL([string] $environment, [string] $brand)
{
    [string] $sqlUse = "use " + (Get-LaunchDatabase -env $environment -brand $brand -DatabaseType enova | % { $_.DatabaseName })
    [string] $sqlIdentifier = Get-LaunchSyncFolder -env $environment -brand $brand | % { $_.SqlCommand }

    [string] $sql = $sqlUse + "`n" + $sqlIdentifier

    if ($Verbose)
    {
        Write-Host -f Cyan (">>>" + $sql + "<<<")
    }

    return $sql
}

function GatherIdentifiers([string] $SQLServer, [string] $username)
{
    [string] $sourcepath = "\\" + $SQLServer + "\c`$\Users\" + $username + "\Documents\identifier*.txt"
    dir $sourcepath | % {
        Write-Host ("Copying: '" + $_ + "' -> .")
        copy $_ .
    }
}
