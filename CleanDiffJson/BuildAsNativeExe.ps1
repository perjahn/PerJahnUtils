Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    [string[]] $folders = ".vs", "obj", "bin"
    dir -Force | ? { $folders -contains $_.Name } | % {
        Write-Host ("Deleting folder: '" + $_.Name + "'")
        rd -Recurse -Force $_.Name
    }

    [string] $nugetconfig = '<configuration><packageSources><add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" /></packageSources></configuration>'
    Set-Content "nuget.config" $nugetconfig

    [string] $projectfile = GetOneFile "*.csproj"

    [string] $include = "Microsoft.DotNet.ILCompiler"
    [string] $version = "1.0.0-alpha-27515-01"

    AddPackageReference $projectfile $include $version

    dotnet publish -c Release -r win-x64
    if (!$?)
    {
        Revert
        exit 1
    }

    [string] $outfile = GetOneFile "bin\Release\netcoreapp2.2\win-x64\native\*.exe"
    if (!$outfile)
    {
        Revert
        exit 1
    }

    Write-Host ("The file is here: " + $outfile) -f Green

    Revert
}

function Revert()
{
    RemovePackageReference $projectfile $include $version
    del "nuget.config"
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

    #if (!($xml.DocumentElement.ItemGroup.PackageReference | ? { $_.Attributes["Include"].Value -eq $include -and $_.Attributes["Version"].Value -eq $version }))
    #{
        Write-Host ("Adding reference: '" + $include + "', '" + $version + "'")
        $packageReference = $xml.CreateElement("PackageReference")
        $packageReference.SetAttribute("Include", $include)
        $packageReference.SetAttribute("Version", $version)
        $xml.DocumentElement.ItemGroup.AppendChild($packageReference) | Out-Null

        SaveXml $filename $xml
    #}
}

function RemovePackageReference([string] $filename, [string] $include, [string] $version)
{
    Write-Host ("Reading project: '" + $filename + "'")
    [xml] $xml = Get-Content $filename

    [bool] $modified = $false
    $xml.DocumentElement.ItemGroup.PackageReference | ? { $_.Attributes["Include"].Value -eq $include -and $_.Attributes["Version"].Value -eq $version } | % {
        Write-Host ("Removing reference: '" + $include + "', '" + $version + "'")
        $_.ParentNode.RemoveChild($_) | Out-Null
        $modified = $true
    }
    if ($modified)
    {
        SaveXml $filename $xml
    }
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
