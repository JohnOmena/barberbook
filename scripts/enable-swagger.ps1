# scripts/enable-swagger.ps1
# Habilita Swagger e o Painel Web via docker-compose.override.yml
# Faz rebuild, sobe os serviços e abre as UIs (Swagger + Web).

[CmdletBinding()]
param(
  [string]$ProjectRoot = "C:\dev\barberbook",
  [int]$WaitSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$composeMain = Join-Path $ProjectRoot "docker-compose.yml"
$composeOverride = Join-Path $ProjectRoot "docker-compose.override.yml"

if (-not (Test-Path $composeMain)) { throw "Arquivo não encontrado: $composeMain" }

# 1) Criar/atualizar override para habilitar Development e apontar Painel -> API interna
$overrideContent = @"
services:
  barberbook-api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
  barberbook-web:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:5161
      - Api__BaseUrl=http://barberbook-api:8080
"@

if (Test-Path $composeOverride) {
  $ts = Get-Date -Format "yyyyMMdd-HHmmss"
  $backup = "$composeOverride.bak-$ts"
  Copy-Item $composeOverride $backup -Force
  Write-Host "Backup criado: $backup"
}

$overrideContent | Set-Content -Encoding UTF8 $composeOverride
Write-Host "docker-compose.override.yml atualizado em: $composeOverride"

# 2) Build + Up (usando o override)
Write-Host "Fazendo build das imagens (api + web)..."
docker compose -f $composeMain -f $composeOverride build --pull barberbook-api barberbook-web | Out-Host

Write-Host "Subindo serviços..."
docker compose -f $composeMain -f $composeOverride up -d | Out-Host

# Portas (permitir sobrescrita por env)
$apiPort = 8080; if ($env:API_HOST_PORT) { try { $apiPort = [int]$env:API_HOST_PORT } catch {} }
$webPort = 5161; if ($env:WEB_HOST_PORT) { try { $webPort = [int]$env:WEB_HOST_PORT } catch {} }

# 3a) Aguardar Swagger ficar disponível
$swaggerJsonUrl = "http://localhost:$apiPort/swagger/v1/swagger.json"
$swaggerUiUrl   = "http://localhost:$apiPort/swagger"

Write-Host "Aguardando Swagger responder em: $swaggerJsonUrl (timeout: $WaitSeconds s)"
$sw = $null
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $sw -and $stopwatch.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try { $sw = Invoke-RestMethod $swaggerJsonUrl -TimeoutSec 5 } catch { Start-Sleep -Seconds 2 }
}
$stopwatch.Stop()

if (-not $sw) {
  Write-Warning "Swagger não respondeu dentro de $WaitSeconds s.
Verifique logs com: docker compose logs -f barberbook-api"
  exit 1
}

Write-Host "Swagger OK!"
Write-Host "Abrindo UI do Swagger no navegador..."
Start-Process $swaggerUiUrl | Out-Null

# 3b) Aguardar Web UI e abrir
$webUrl = "http://localhost:$webPort/Admin/Login"
Write-Host "Aguardando Web UI responder em: $webUrl (timeout: $WaitSeconds s)"
$webOk = $false
$stopwatchWeb = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $webOk -and $stopwatchWeb.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try {
    $resp = Invoke-WebRequest -Uri $webUrl -TimeoutSec 5 -UseBasicParsing
    if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { $webOk = $true }
  } catch { Start-Sleep -Seconds 2 }
}
$stopwatchWeb.Stop()

if ($webOk) {
  Write-Host "Web UI OK!"; Write-Host "Abrindo painel..."; Start-Process $webUrl | Out-Null
} else {
  Write-Warning "Web UI não respondeu dentro de $WaitSeconds s. Veja: docker compose logs -f barberbook-web"
}

# 4) Listar endpoints POST e filtrar os prováveis de agendamento
Write-Host ""
Write-Host "==== ENDPOINTS POST DETECTADOS ===="
$posts = @()
$sw.paths.PSObject.Properties | ForEach-Object {
  $path = $_.Name
  $node = $_.Value
  $post = $node.PSObject.Properties['post']
  if ($null -ne $post -and $null -ne $post.Value) {
    $sumProp  = $post.Value.PSObject.Properties['summary']
    $opIdProp = $post.Value.PSObject.Properties['operationId']
    $sum  = if ($sumProp)  { $sumProp.Value }  else { '' }
    $opId = if ($opIdProp) { $opIdProp.Value } else { '' }
    $posts += [pscustomobject]@{ Path = $path; Sum = $sum; OpId = $opId }
  }
}
$posts | Sort-Object Path | Format-Table -AutoSize

Write-Host ""
Write-Host "==== POSSÍVEIS ENDPOINTS DE AGENDAMENTO (appoint|book|schedule|agend) ===="
$posts | Where-Object { $_.Path -match 'appoint|book|schedule|agend' -or ($_.Sum -match 'appoint|book|schedule|agend') -or ($_.OpId -match 'appoint|book|schedule|agend') } |
  Sort-Object Path | Format-Table -AutoSize

Write-Host ""
Write-Host "Pronto! Use o Swagger para testar e acesse o painel em $webUrl"
Write-Host "Dica: você pode ajustar portas via env: API_HOST_PORT e WEB_HOST_PORT."

