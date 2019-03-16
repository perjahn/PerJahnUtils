Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Clean

    [string] $include = "Microsoft.DotNet.ILCompiler"
    [string] $version = "1.0.0-alpha-27515-01"

    $files = @(dir -r -i "*.csproj" | ? { $_.Length -lt 1kb })
    Write-Host ("Found " + $files.Count + " projects.")

    foreach ($projectfile in $files)
    {
        [string] $folder = Split-Path $projectfile.FullName

        [string] $nugetfile = Join-Path $folder "nuget.config"
        [string] $nugetconfig = '<configuration><packageSources><add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" /></packageSources></configuration>'
        Write-Host ("Creating: '" + $nugetfile + "'")
        Set-Content $nugetfile $nugetconfig

        AddPackageReference $projectfile $include $version
    }

    $builds = @()

    foreach ($projectfile in $files)
    {
        [string] $folder = Split-Path $projectfile.FullName

        $builds += Start-Process -FilePath dotnet -ArgumentList "publish","-c","Release","-r","win-x64" -WorkingDirectory $folder -PassThru
    }

    Write-Host ("Running: " + $builds.Count + " builds.") -f Cyan

    do
    {
        Start-Sleep 5
        Write-Host ("Running: " + @($builds | ? { !$_.HasExited }).Count + " builds.") -f Cyan
    }
    while (@($builds | ? { !$_.HasExited }).Count -gt 0)

    foreach ($projectfile in $files)
    {
        RemovePackageReference $projectfile $include $version

        [string] $folder = Split-Path $projectfile.FullName
        [string] $nugetfile = Join-Path $folder "nuget.config"
        del $nugetfile
    }

    foreach ($projectfile in $files)
    {
        [string] $outfile = GetOneFile (Join-Path (Split-Path $projectfile) "bin\Release\netcoreapp2.2\win-x64\native\*.exe")
        if (!$outfile)
        {
            Write-Host ("Couldn't find any output file for project: '" + $projectfile + "'")
        }
        else
        {
            [string] $outputfolder = "PerJahnUtils"
            if (!(Test-Path $outputfolder))
            {
                Write-Host ("Creating folder: '" + $outputfolder + "'")
                md $outputfolder
            }
            Write-Host ("Copying file: '" + $outfile + "' -> '" + $outputfolder + "'") -f Green
            copy $outfile $outputfolder -Force
        }
    }
}

function Clean()
{
    [string[]] $folders = ".vs", "obj", "bin"
    dir -Recurse -Force | ? { $folders -contains $_.Name } | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse -Force $_.FullName
    }
}

function GetOneFile([string] $pattern)
{
    [string] $typename = [IO.Path]::GetExtension($pattern).Substring(1)
    if (!(Test-Path $pattern))
    {
        Write-Host ("Couldn't find any " + $typename + " file: '" + $pattern + "'") -f Red
        return $null
    }
    $files = @(dir $pattern)
    if ($files.Count -lt 1)
    {
        Write-Host ("Couldn't find any " + $typename + " file: '" + $pattern + "'") -f Red
        return $null
    }
    if ($files.Count -gt 1)
    {
        Write-Host ("Too many " + $typename + " files found (" + (($files | % { $_.Name }) -join ", ") + "): '" + $pattern + "'") -f Red
        return $null
    }
    return $files[0]
}

function AddPackageReference([string] $filename, [string] $include, [string] $version)
{
    Write-Host ("Reading project: '" + $filename + "'")
    [xml] $xml = Get-Content $filename

    [string] $xpath = "ItemGroup/PackageReference[@Include='" + $include + "'][@Version='" + $version + "']"
    $nodes = $xml.DocumentElement.SelectNodes($xpath)

    if ($nodes.Count -gt 0)
    {
        Write-Host ("PackageReference already exists.")
        return
    }

    if ($xml.DocumentElement.SelectNodes("ItemGroup").Count -eq 0)
    {
        Write-Host ("Adding ItemGroup")
        $itemGroup = $xml.CreateElement("ItemGroup")
        $xml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }

    Write-Host ("Adding PackageReference: '" + $include + "', '" + $version + "'")
    $packageReference = $xml.CreateElement("PackageReference")
    $packageReference.SetAttribute("Include", $include)
    $packageReference.SetAttribute("Version", $version)
    $xml.DocumentElement.SelectNodes("ItemGroup")[0].AppendChild($packageReference) | Out-Null

    SaveXml $filename $xml
}

function RemovePackageReference([string] $filename, [string] $include, [string] $version)
{
    Write-Host ("Reading project: '" + $filename + "'")
    [xml] $xml = Get-Content $filename

    [string] $xpath = "ItemGroup/PackageReference[@Include='" + $include + "'][@Version='" + $version + "']"
    $nodes = $xml.DocumentElement.SelectNodes($xpath)

    if ($nodes.Count -eq 0)
    {
        Write-Host ("No PackageReference to remove.")
        return
    }

    Write-Host ("Found " + $nodes.Count + " PackageReference to remove.")

    foreach ($node in $nodes)
    {
        Write-Host ("Removing PackageReference: '" + $include + "', '" + $version + "'")
        $node.ParentNode.RemoveChild($node) | Out-Null
    }


    [string] $xpath = "ItemGroup[not(*)]"
    $nodes = $xml.DocumentElement.SelectNodes($xpath)
    
    Write-Host ("Found " + $nodes.Count + " empty ItemGroup to remove.")

    foreach ($node in $nodes)
    {
        Write-Host ("Removing empty ItemGroup.")
        $node.ParentNode.RemoveChild($node) | Out-Null
    }

    SaveXml $filename $xml
}

function SaveXml([string] $filename, $xml)
{
    Write-Host ("Saving project file: '" + $filename + "'")
    [Xml.XmlWriterSettings] $settings = New-Object Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object Text.UTF8Encoding($false)
    $settings.OmitXmlDeclaration = $true
    $writer = [Xml.XmlWriter]::Create($filename, $settings)
    $xml.Save($writer)
    $writer.Dispose()
}

Main
