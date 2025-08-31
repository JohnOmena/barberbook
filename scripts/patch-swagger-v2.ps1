# scripts/patch-swagger-v2.ps1
[CmdletBinding()]
param(
  [string]$ProjectRoot = "C:\dev\barberbook",
  [int]$WaitSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Indent([string]$line) {
  return ($line -replace '(^\s*).*','$1')
}

$program = Join-Path $ProjectRoot "BarberBook.Api\Program.cs"
if (-not (Test-Path $program)) { throw "Program.cs não encontrado: $program" }

# 1) Carrega Program.cs
$content  = Get-Content -Raw -Path $program
$original = $content
$changed  = $false

# 2) Garante AddEndpointsApiExplorer / AddSwaggerGen antes do builder.Build()
if ($content -notmatch 'AddEndpointsApiExplorer' -or $content -notmatch 'AddSwaggerGen') {
  $anchor = 'var app = builder.Build();'
  $pos = $content.IndexOf($anchor)
  if ($pos -lt 0) { $anchor = 'builder.Build()'; $pos = $content.IndexOf($anchor) }
  if ($pos -lt 0) { throw "Não achei 'builder.Build()' no Program.cs para inserir os services do Swagger." }

  $servicesSnippet = @"
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

"@

  $content = $content.Insert($pos, $servicesSnippet)
  $changed = $true
}

# 3) Garante app.UseSwagger() / app.UseSwaggerUI() no pipeline
$hasUseSwagger   = ($content -match 'app\.UseSwagger\s*\(')
$hasUseSwaggerUI = ($content -match 'app\.UseSwaggerUI\s*\(')

if (-not $hasUseSwagger) {
  $lines = $content -split "`r?`n"

  # Se já existir UseSwaggerUI, insere UseSwagger logo acima mantendo a indentação
  $uiIndex = -1
  for ($i=0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match 'app\.UseSwaggerUI\s*\(') { $uiIndex = $i; break }
  }

  if ($uiIndex -ge 0) {
    $indent = Get-Indent $lines[$uiIndex]
    $useLine = "${indent}app.UseSwagger();"
    if ($uiIndex -eq 0) {
      $lines = ,$useLine + $lines
    } else {
      $before = $lines[0..($uiIndex-1)]
      $after  = $lines[$uiIndex..($lines.Length-1)]
      $lines  = $before + $useLine + $after
    }
    $content = ($lines -join "`r`n")
    $changed = $true
  } else {
    # Não tem UI: insere bloco Development completo antes de app.Map* ou app.Run(
    $anchorIdx = -1
    for ($i=0; $i -lt $lines.Length; $i++) {
      if ($lines[$i] -match 'app\.Map' -or $lines[$i] -match 'app\.Run\s*\(') { $anchorIdx = $i; break }
    }
    if ($anchorIdx -lt 0) { throw "Não achei ponto (app.Map*/app.Run) para inserir bloco de Swagger no pipeline." }

    $block = @"
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
"@ -split "`r?`n"

    $before = $lines[0..($anchorIdx-1)]
    $after  = $lines[$anchorIdx..($lines.Length-1)]
    $lines  = $before + $block + $after
    $content = ($lines -join "`r`n")
    $changed = $true
  }
}

# 4) Salva se mudou (com backup)
if ($changed) {
  $backup = "$program.bak"
  Copy-Item $program $backup -Force
  Set-Content -Path $program -Value $content -Encoding UTF8
  Write-Host "Program.cs atualizado. Backup criado: $backup"
} else {
  Write-Host "Nenhuma alteração necessária no Program.cs."
}

# 5) Rebuild e subir
Write-Host "Recompilando imagem barberbook-api..."
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") build barberbook-api

Write-Host "Subindo containers..."
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") up -d

# 6) Espera Swagger responder
$swaggerUrl = "http://localhost:8080/swagger/v1/swagger.json"
Write-Host "Aguardando Swagger em: $swaggerUrl (timeout: $WaitSeconds s)"
$sw = $null
$timer = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $sw -and $timer.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try { $sw = Invoke-RestMethod $swaggerUrl -TimeoutSec 5 } catch { Start-Sleep 2 }
}
$timer.Stop()

if (-not $sw) {
  Write-Warning "Swagger não respondeu em $WaitSeconds s."
  Write-Host "Veja os logs com: docker compose logs -f barberbook-api"
  return
}

Write-Host "OK! Swagger respondeu em $([int]$timer.Elapsed.TotalSeconds)s"
Write-Host "Abra: http://localhost:8080/swagger`n"

# 7) Lista endpoints POST e prováveis de agendamento
$posts = @()
$sw.paths.PSObject.Properties.Name | ForEach-Object {
  $p = $_; $node = $sw.paths.$p
  if ($null -ne $node.post) {
    $posts += [pscustomobject]@{
      Path    = $p
      Summary = $node.post.summary
      OpId    = $node.post.operationId
    }
  }
}

"== ENDPOINTS POST =="
$posts | Sort-Object Path | Format-Table -AutoSize

""
"== Prováveis de agendamento (appoint|book|schedule|agend) =="
$posts | Where-Object {
  $_.Path -match 'appoint|book|schedule|agend' -or
  ($_.Summary -and $_.Summary -match 'appoint|book|schedule|agend') -or
  ($_.OpId -and $_.OpId -match 'appoint|book|schedule|agend')
} | Sort-Object Path | Format-Table -AutoSize
