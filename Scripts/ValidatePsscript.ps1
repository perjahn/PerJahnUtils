Set-StrictMode -v latest
$ErrorActionPreference = "Stop"

function Main([string[]] $mainargs)
{
  [System.String[]] $path = $mainargs

  $files = @(dir $path -Recurse)

  Write-Host ("Found " + $files.Count + " files.")

  [int] $invalidFiles = 0
  [int] $errorCount = 0

  foreach ($file in $files)
  {
    [string] $filename = $file.FullName
    $errors = $null
    $content = Get-Content $filename
    if (!$content)
    {
      continue
    }

    [System.Management.Automation.PsParser]::Tokenize($content, [ref] $errors) | Out-Null

    if ($errors)
    {
      $invalidFiles += 1
      foreach ($error in $errors)
      {
        Write-Host ($filename + " (" + $error.Token.StartLine + "," + $error.Token.StartColumn + "): " + $error.Message)
        $errorCount += 1
      }
    }
  }

  Write-Host ("" + $invalidFiles + " scripts contain " + $errorCount + " syntax errors.")
}

Main $args
