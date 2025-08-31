# up.ps1
# Sobe os serviços com docker compose
$ROOT = $env:BARBERBOOK_ROOT
if (-not $ROOT) {
  if ($PSScriptRoot) { $ROOT = (Resolve-Path "$PSScriptRoot\..").Path }
  else { $ROOT = (Resolve-Path (Join-Path (Get-Location).Path "..")).Path }
}
$compose = Join-Path $ROOT "docker-compose.yml"
$envFile = Join-Path $ROOT ".env"
if (-not (Test-Path $envFile)) {
  Write-Host "Faltou .env. Rode scripts\ensure-env.ps1 e edite GEMINI_API_KEY." -ForegroundColor Red
  exit 1
}
docker compose -f $compose --env-file $envFile up -d
docker compose -f $compose --env-file $envFile ps
$port = (Get-Content $envFile | Where-Object { $_ -match '^\s*N8N_HOST_PORT\s*=' } |
  ForEach-Object { $_.Split('=')[-1].Trim() }) | Select-Object -First 1
if (-not $port -or -not ($port -match '^\d+$')) { $port = '5678' }
Write-Host "`nAcesse n8n: http://localhost:$port" -ForegroundColor Cyan
