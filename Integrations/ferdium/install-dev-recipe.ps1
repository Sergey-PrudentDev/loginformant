param()

$source = Join-Path $PSScriptRoot 'loginformant'
$targetRoot = Join-Path $env:APPDATA 'Ferdium\recipes\dev'
$target = Join-Path $targetRoot 'loginformant'

if (-not (Test-Path $source)) {
    throw "LogInformant Ferdium recipe source not found at $source"
}

New-Item -ItemType Directory -Force -Path $targetRoot | Out-Null
if (Test-Path $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}
Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
Write-Host "Installed LogInformant Ferdium dev recipe to $target"
Write-Host "Restart Ferdium, open Add Service, then look under the Custom Services tab for LogInformant."
Write-Host "LogInformant will only appear in the main All services catalog after it is submitted upstream to Ferdium."
