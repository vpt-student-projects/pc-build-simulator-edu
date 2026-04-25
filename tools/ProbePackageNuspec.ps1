param([Parameter(Mandatory=$true)][string]$Id, [Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference = "Stop"
$d = Join-Path $env:TEMP "nuspec_probe_$Id"
Remove-Item -Recurse -Force $d -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $d | Out-Null
$zip = Join-Path $d "p.zip"
Invoke-WebRequest "https://www.nuget.org/api/v2/package/$Id/$Version" -OutFile $zip
Expand-Archive $zip -DestinationPath $d -Force
$n = Get-ChildItem $d -Filter "*.nuspec" | Select-Object -First 1
Get-Content $n.FullName -Raw
