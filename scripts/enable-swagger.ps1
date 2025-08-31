# scripts/enable-swagger.ps1
# Habilita Swagger no container do barberbook-api via docker-compose.override.yml
# Faz rebuild, sobe os serviços e extrai os endpoints POST do Swagger.

[CmdletBinding()]
param(
  [string]$ProjectRoot = "C:\dev\barberbook",
  [int]$WaitSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$composeMain = Join-Path $ProjectRoot "docker-compose.yml"
$composeOverride = Join-Path $ProjectRoot "docker-compose.override.yml"

if (-not (Test-Path $composeMain)) {
  throw "Arquivo não encontrado: $composeMain"
}

# 1) Criar/atualizar override para habilitar Swagger (Development) e garantir URL na 8080
$overrideContent = @"
services:
  barberbook-api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
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
Write-Host "Fazendo build da imagem barberbook-api..."
docker compose -f $composeMain -f $composeOverride build --pull barberbook-api | Out-Host

Write-Host "Subindo serviços..."
docker compose -f $composeMain -f $composeOverride up -d | Out-Host

# 3) Aguardar Swagger ficar disponível
$swaggerJsonUrl = "http://localhost:8080/swagger/v1/swagger.json"
$swaggerUiUrl   = "http://localhost:8080/swagger"

Write-Host "Aguardando Swagger responder em: $swaggerJsonUrl (timeout: $WaitSeconds s)"
$sw = $null
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $sw -and $stopwatch.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try {
    $sw = Invoke-RestMethod $swaggerJsonUrl -TimeoutSec 5
  } catch {
    Start-Sleep -Seconds 2
  }
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

# 4) Listar endpoints POST e filtrar os prováveis de agendamento
Write-Host ""
Write-Host "==== ENDPOINTS POST DETECTADOS ===="
$posts = @()
$sw.paths.PSObject.Properties.Name | ForEach-Object {
  $path = $_
  $node = $sw.paths.$path
  if ($null -ne $node.post) {
    $posts += [pscustomobject]@{
      Path   = $path
      Sum    = $node.post.summary
      OpId   = $node.post.operationId
    }
  }
}
$posts | Sort-Object Path | Format-Table -AutoSize

Write-Host ""
Write-Host "==== POSSÍVEIS ENDPOINTS DE AGENDAMENTO (appoint|book|schedule|agend) ===="
$posts | Where-Object { $_.Path -match 'appoint|book|schedule|agend' -or ($_.Sum -match 'appoint|book|schedule|agend') -or ($_.OpId -match 'appoint|book|schedule|agend') } |
  Sort-Object Path | Format-Table -AutoSize

Write-Host ""
Write-Host "Pronto! Se algum endpoint de criação aparecer acima, use-o para o POST do agendamento."
Write-Host "Dica: você pode inspecionar o schema do corpo pelo Swagger UI em $swaggerUiUrl."
