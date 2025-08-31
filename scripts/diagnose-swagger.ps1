# scripts/diagnose-swagger.ps1
[CmdletBinding()]
param(
  [string]$ProjectRoot = "C:\dev\barberbook",
  [int]$WaitSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$composeMain     = Join-Path $ProjectRoot "docker-compose.yml"
$composeOverride = Join-Path $ProjectRoot "docker-compose.override.yml"
$programCs       = Join-Path $ProjectRoot "BarberBook.Api\Program.cs"

if (-not (Test-Path $composeMain))     { throw "Não encontrei $composeMain" }
if (-not (Test-Path $composeOverride)) { Write-Warning "docker-compose.override.yml não encontrado. Vou continuar." }

Write-Host ">> Checando variável dentro do container..."
$envVal = ""
try {
  $envVal = docker compose -f $composeMain -f $composeOverride exec -T barberbook-api printenv ASPNETCORE_ENVIRONMENT 2>$null
} catch {}
if (-not $envVal) {
  Write-Warning "Não consegui ler ASPNETCORE_ENVIRONMENT de dentro do container."
} else {
  Write-Host "ASPNETCORE_ENVIRONMENT = $envVal"
}

Write-Host "`n>> Conferindo se o código registrou Swagger (Program.cs)..."
$hasAddSwagger  = (Test-Path $programCs) -and (Select-String -Path $programCs -SimpleMatch -Pattern 'AddSwaggerGen' -Quiet)
$hasEndpoints   = (Test-Path $programCs) -and (Select-String -Path $programCs -SimpleMatch -Pattern 'AddEndpointsApiExplorer' -Quiet)
$hasUseSwagger  = (Test-Path $programCs) -and (Select-String -Path $programCs -Pattern 'UseSwagger\(' -Quiet)
$hasSwaggerUI   = (Test-Path $programCs) -and (Select-String -Path $programCs -Pattern 'UseSwaggerUI\(' -Quiet)

[pscustomobject]@{
  ProgramCsFound         = (Test-Path $programCs)
  Has_AddEndpointsApiExp = $hasEndpoints
  Has_AddSwaggerGen      = $hasAddSwagger
  Has_UseSwagger         = $hasUseSwagger
  Has_UseSwaggerUI       = $hasSwaggerUI
} | Format-List

if (-not ($hasAddSwagger -and $hasEndpoints -and $hasUseSwagger -and $hasSwaggerUI)) {
  Write-Warning @"
Faltam chamadas de Swagger no Program.cs. Adicione algo como:

// NO topo, após criar o builder:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// NO pipeline, antes de Mapear endpoints:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
"@
  Write-Host "Depois rode: docker compose build barberbook-api && docker compose up -d"
}

Write-Host "`n>> Tentando baixar Swagger..."
$swaggerJsonUrl = "http://localhost:8080/swagger/v1/swagger.json"
$sw = $null
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $sw -and $stopwatch.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try { $sw = Invoke-RestMethod $swaggerJsonUrl -TimeoutSec 5 } catch { Start-Sleep 2 }
}
$stopwatch.Stop()

if ($sw) {
  Write-Host "OK: Swagger respondeu em $([int]$stopwatch.Elapsed.TotalSeconds)s"
  Write-Host "Abra: http://localhost:8080/swagger"
  # lista os POST
  $posts = @()
  $sw.paths.PSObject.Properties.Name | ForEach-Object {
    $p = $_; $node = $sw.paths.$p
    if ($null -ne $node.post) {
      $posts += [pscustomobject]@{ Path=$p; Summary=$node.post.summary; OpId=$node.post.operationId }
    }
  }
  Write-Host "`n== ENDPOINTS POST =="
  $posts | Sort-Object Path | Format-Table -AutoSize
  Write-Host "`n== Prováveis de agendamento (appoint|book|schedule|agend) =="
  $posts | Where-Object { $_.Path -match 'appoint|book|schedule|agend' -or $_.Summary -match 'appoint|book|schedule|agend' -or $_.OpId -match 'appoint|book|schedule|agend' } |
    Sort-Object Path | Format-Table -AutoSize
} else {
  Write-Warning "Swagger não respondeu em $WaitSeconds s. Veja os logs:"
  Write-Host "docker compose logs -f barberbook-api"
  Write-Host "Ou confira as envs dentro do container:"
  Write-Host "docker compose exec -T barberbook-api env | sort"
}
