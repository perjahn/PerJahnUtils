# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Clean

    Generate-BuildFile "all.build"
}

function Clean()
{
    $files = @(dir -r -i obj,bin,Debug,Release)
    $files | ? { $_.Attributes -band [IO.FileAttributes]::Directory } | % {
        Write-Host ("Deleting folder: '" + $_.FullName + "'")
        rd -Recurse -Force $_.FullName -ErrorAction SilentlyContinue
    }
}

function Generate-BuildFile([string] $buildfile)
{
    $files = @(dir -r -i *.sln | ? { !($_.Attributes -band [IO.FileAttributes]::Directory) })

    Write-Host ("Found " + $files.Count + " solutions.")

    [string[]] $xml = @()
    $xml += '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">'
    $xml += '  <Target Name="Build">'

    $files | % {
        [string] $filename = $_
        if ($filename.StartsWith('.\'))
        {
            $filename = $filename.Substring(2)
        }

        $xml += '    <MSBuild Projects="' + $filename + '" Properties="Configuration=Release" ContinueOnError="true" />'
    }

    $xml += '  </Target>'
    $xml += '</Project>'

    Write-Host ("Saving build file: '" + $buildfile + "'")
    sc $buildfile $xml
}

Main
