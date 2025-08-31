# logs.ps1
# Segue logs uteis
$ROOT = $env:BARBERBOOK_ROOT
if (-not $ROOT) {
  if ($PSScriptRoot) { $ROOT = (Resolve-Path "$PSScriptRoot\..").Path }
  else { $ROOT = (Resolve-Path (Join-Path (Get-Location).Path "..")).Path }
}
$compose = Join-Path $ROOT "docker-compose.yml"
$envFile = Join-Path $ROOT ".env"
docker compose -f $compose --env-file $envFile logs -f postgres redis barberbook-api waha n8n
