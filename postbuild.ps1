Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    [string] $include = "Microsoft.DotNet.ILCompiler"
    [string] $version = "1.0.0-alpha-27519-01"

    $files = @(dir -r -i "*.csproj" | ? { $_.Length -lt 1kb })
    Write-Host ("Found " + $files.Count + " projects.")

    foreach ($projectfile in $files)
    {
        RemovePackageReference $projectfile $include $version

        [string] $folder = Split-Path $projectfile.FullName
        [string] $nugetfile = Join-Path $folder "nuget.config"
        del $nugetfile
    }


    if ($env:buildconfig)
    {
        [string] $buildconfig = $env:buildconfig
    }
    else
    {
        [string] $buildconfig = "Release"
    }

    [string] $artifactfolder = "PerJahnUtils"

    Gather-Artifacts $buildconfig $artifactfolder

    Compress-Artifacts $artifactfolder

    Write-Host ("Current time: " + (Get-Date -f "yyyy-MM-dd HH:mm:ss"))
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

function Gather-Artifacts([string] $buildconfig, [string] $artifactfolder)
{
    Write-Host ("Artifact folder: '" + $artifactfolder + "'")
    Write-Host ("Build config: '" + $buildconfig + "'")

    if (Test-Path $artifactfolder)
    {
        Write-Host ("Deleting old artifact folder: '" + $artifactfolder + "'")
        rd -Recurse $artifactfolder
    }

    Write-Host ("Creating artifact folder: '" + $artifactfolder + "'")
    md $artifactfolder | Out-Null

    $exefiles = dir -r -i *.exe -Exclude *.vshost.exe,nuget.exe | ? { !$_.FullName.Contains([IO.Path]::DirectorySeparatorChar + "obj" + [IO.Path]::DirectorySeparatorChar) }
    $exefiles | % {
        [string] $source = $_.FullName
        Write-Host ("Copying file: '" + $source + "' -> '" + $artifactfolder + "'")
        copy $source $artifactfolder
    }
}

function Compress-Artifacts([string] $artifactfolder)
{
    [string] $sevenzippath = "C:\Program Files\7-Zip\7z.exe"
    if (Test-Path $sevenzippath)
    {
        Set-Alias zip $sevenzippath
    }
    else
    {
        Set-Alias zip 7z
    }

    [string] $outfile = "PerJahnUtils.zip"
    if (Test-Path $outfile)
    {
        Write-Host ("Deleting old archive file: '" + $outfile + "'")
        del $outfile
    }

    Write-Host ("Compressing " + @(dir $artifactfolder).Count + " exe tools to " + $outfile + "...")
    zip a -mx9 $outfile (Join-Path $artifactfolder *.exe)
    if (!$? -or !(Test-Path $outfile))
    {
        Write-Host ("Couldn't compress artifacts.") -f Red
        exit 1
    }

    Write-Host ("Produced artifact: " + $outfile) -f Green
}

Main
