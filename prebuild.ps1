# edit with: powershell_ise.exe

Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main()
{
    Create-BuildFile "all.build"
}

function Create-BuildFile([string] $buildfile)
{
    $files = dir -r -i *.sln

    [string] $xml = '<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">' + "`n" + '  <Target Name="Build">' + "`n"

    $files | % {
        [string] $filename = $_
        if ($filename.StartsWith('.\'))
        {
            $filename = $filename.Substring(2)
        }

        $xml += '    <MSBuild Projects="' + $filename + '" Properties="Configuration=Release" ContinueOnError="true" />' + "`n"
    }

    $xml += '  </Target>' + "`n" + '</Project>' + "`n"

    sc $xml $buildfile
}

Main
