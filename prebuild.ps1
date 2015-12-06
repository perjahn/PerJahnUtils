# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Create-BuildFile "all.build"
}

function Create-BuildFile([string] $buildfile)
{
    $files = @(dir -r -i *.sln)

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
