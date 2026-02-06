param(
  [string]$Directory = "."
)

$utf8Bom = New-Object System.Text.UTF8Encoding($true)

Get-ChildItem -Path $Directory -Recurse -Filter *.xaml |
  Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' } |
  ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $fixed = $content.Replace("`r`n", "`n")
    if ($fixed -ne $content) {
      [System.IO.File]::WriteAllText($_.FullName, $fixed, $utf8Bom)
    }
  }
