Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Download-NugetPackage
    Update-Projects
}

function Download-NugetPackage()
{
    [string] $url = "https://www.nuget.org/api/v2/package/StyleCop.MSBuild/5.0.0"
    [string] $zipfile = "StyleCop.MSBuild.5.0.0.zip"
    [string] $folder = Join-Path "packages" "StyleCop.MSBuild.5.0.0"

    if (Test-Path $folder)
    {
        Write-Host ("StyleCop already downloaded.")
        return
    }

    Write-Host ("Downloading: '" + $url + "' -> '" + $zipfile + "'")
    Invoke-WebRequest $url -OutFile $zipfile

    Write-Host ("Extracting: '" + $zipfile + "' -> '" + $folder + "'")
    Expand-Archive $zipfile -DestinationPath $folder
}

function Update-Projects()
{
    $files = @(dir -r -i *.*proj)
    Write-Host ("Found " + $files.Count + " projects.")

    foreach ($file in $files)
    {
        [string] $filename = $file.FullName.Substring((pwd).Path.Length+1)

        Write-Host ("Reading: '" + $filename + "'")
        [xml] $xml = Get-Content $filename

        [int] $depth = @($filename.ToCharArray() | ? { $_ -eq "\" }).Count
        [string] $projectPath = ("..\" * $depth) + "packages\StyleCop.MSBuild.5.0.0\build\StyleCop.MSBuild.targets"

        if (!($xml.DocumentElement.ChildNodes | ? { $_.Name -eq "Import" -and ($_.Attributes | ? { $_.Name -eq "Project" }) -and $_.Project -eq $projectPath }))
        {
            $import = $xml.CreateElement("Import", $xml.DocumentElement.NamespaceURI)
            $import.SetAttribute("Project", $projectPath)
            $xml.DocumentElement.AppendChild($import) | Out-Null

            Write-Host ("Saving: '" + $filename + "'")
            $xml.Save($filename)
        }
    }
}

Main
