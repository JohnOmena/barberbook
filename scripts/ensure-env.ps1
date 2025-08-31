# ensure-env.ps1
# Cria .env a partir de .env.example (se faltar)
$ROOT = $env:BARBERBOOK_ROOT
if (-not $ROOT) {
  if ($PSScriptRoot) { $ROOT = (Resolve-Path "$PSScriptRoot\..").Path }
  else { $ROOT = (Resolve-Path (Join-Path (Get-Location).Path "..")).Path }
}
$envExample = Join-Path $ROOT ".env.example"
$envFile    = Join-Path $ROOT ".env"
if (-not (Test-Path $envFile)) {
  if (-not (Test-Path $envExample)) {
@"
POSTGRES_USER=barber
POSTGRES_PASSWORD=barberpass
POSTGRES_DB=barberbook
POSTGRES_PORT=5432
REDIS_HOST=redis
REDIS_PORT=6379
API_BASE_URL=http://barberbook-api:8080
WAHA_BASE_URL=http://waha:3000
N8N_PORT=5678
GEMINI_API_KEY=REPLACE_GEMINI_API_KEY
API_HOST_PORT=8080
WAHA_HOST_PORT=3000
REDIS_HOST_PORT=6379
POSTGRES_HOST_PORT=5432
N8N_HOST_PORT=5678
"@ | Set-Content $envExample -Encoding utf8
  }
  Copy-Item $envExample $envFile -Force
  Write-Host "Criado .env a partir de .env.example. Edite GEMINI_API_KEY." -ForegroundColor Yellow
} else {
  Write-Host ".env ja existe." -ForegroundColor Green
}
