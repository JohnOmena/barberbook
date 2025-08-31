# scripts/patch-swagger-doc.ps1
[CmdletBinding()]
param(
  [string]$ProjectRoot = "C:\dev\barberbook",
  [int]$WaitSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$program = Join-Path $ProjectRoot "BarberBook.Api\Program.cs"
if (-not (Test-Path $program)) { throw "Program.cs não encontrado: $program" }

# Lê Program.cs
$content  = Get-Content -Raw -Path $program
$original = $content
$changed  = $false

# 1) Garantir AddEndpointsApiExplorer (se não tiver, insere antes do builder.Build())
if ($content -notmatch 'AddEndpointsApiExplorer') {
  $anchor = 'var app = builder.Build();'
  $pos = $content.IndexOf($anchor)
  if ($pos -lt 0) { $anchor = 'builder.Build()'; $pos = $content.IndexOf($anchor) }
  if ($pos -lt 0) { throw "Não achei 'builder.Build()' no Program.cs." }
  $snippet = "builder.Services.AddEndpointsApiExplorer();`r`n"
  $content = $content.Insert($pos, $snippet)
  $changed = $true
}

# 2) Trocar AddSwaggerGen() “vazio” por AddSwaggerGen(...) com SwaggerDoc("v1", ...)
$patternEmptyAdd = 'builder\.Services\.AddSwaggerGen\s*\(\s*\)\s*;'
if ($content -match $patternEmptyAdd -and $content -notmatch 'SwaggerDoc\s*\(\s*"v1"') {
  $replacement = 'builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "BarberBook API", Version = "v1" }));'
  $content = [regex]::Replace($content, $patternEmptyAdd, $replacement)
  $changed = $true
}
# Se não existe nenhum AddSwaggerGen, adiciona um já com SwaggerDoc antes do builder.Build()
elseif ($content -notmatch 'AddSwaggerGen' -and $content -notmatch 'SwaggerDoc\s*\(\s*"v1"') {
  $anchor = 'var app = builder.Build();'
  $pos = $content.IndexOf($anchor)
  if ($pos -lt 0) { $anchor = 'builder.Build()'; $pos = $content.IndexOf($anchor) }
  if ($pos -lt 0) { throw "Não achei 'builder.Build()' no Program.cs." }
  $snippet = "builder.Services.AddSwaggerGen(c => c.SwaggerDoc(""v1"", new Microsoft.OpenApi.Models.OpenApiInfo { Title = ""BarberBook API"", Version = ""v1"" }));`r`n"
  $content = $content.Insert($pos, $snippet)
  $changed = $true
}

# 3) Garantir app.UseSwagger() no pipeline
if ($content -notmatch 'app\.UseSwagger\s*\(') {
  # Insere antes do UseSwaggerUI (se existir) ou antes do primeiro app.Map/app.Run
  $lines = $content -split "`r?`n"
  $idxUI = ($lines | Select-String -Pattern 'app\.UseSwaggerUI\s*\(' | Select-Object -First 1).LineNumber
  if ($idxUI) {
    $insertAt = $idxUI - 1
  } else {
    $insertAt = (($lines | Select-String -Pattern 'app\.Map|app\.Run\s*\(' | Select-Object -First 1).LineNumber) - 1
  }
  if ($insertAt -lt 0) { throw "Não achei ponto para inserir app.UseSwagger()." }
  $indent = ($lines[$insertAt] -replace '(^\s*).*','$1')
  $lines = $lines[0..$insertAt] + ("$indent" + 'app.UseSwagger();') + $lines[($insertAt+1)..($lines.Length-1)]
  $content = $lines -join "`r`n"
  $changed = $true
}

# 4) Substituir app.UseSwaggerUI(); por versão configurada com endpoint /swagger/v1/swagger.json
$patternUIEmpty = 'app\.UseSwaggerUI\s*\(\s*\)\s*;'
if ($content -match $patternUIEmpty -and $content -notmatch 'SwaggerEndpoint\(') {
  $replacementUI = 'app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberBook API v1"));'
  $content = [regex]::Replace($content, $patternUIEmpty, $replacementUI)
  $changed = $true
}
# Se não existe UI, cria bloco Development com UseSwagger + UseSwaggerUI
elseif ($content -notmatch 'app\.UseSwaggerUI\s*\(') {
  $lines = $content -split "`r?`n"
  $anchorLine = ($lines | Select-String -Pattern 'app\.Map|app\.Run\s*\(' | Select-Object -First 1).LineNumber
  if (-not $anchorLine) { throw "Não achei ponto para inserir UseSwaggerUI." }
  $block = @'
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberBook API v1"));
}
'@ -split "`r?`n"
  $before = $lines[0..($anchorLine-2)]
  $after  = $lines[($anchorLine-1)..($lines.Length-1)]
  $lines  = $before + $block + $after
  $content = $lines -join "`r`n"
  $changed = $true
}

# 5) Salva e faz backup se mudou
if ($changed) {
  $backup = "$program.bak2"
  Copy-Item $program $backup -Force
  Set-Content -Path $program -Value $content -Encoding UTF8
  Write-Host "Program.cs ajustado. Backup: $backup"
} else {
  Write-Host "Nenhuma alteração adicional necessária."
}

# 6) Rebuild + up
Write-Host "Recompilando imagem..."
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") build barberbook-api
Write-Host "Subindo containers..."
docker compose -f (Join-Path $ProjectRoot "docker-compose.yml") up -d

# 7) Testa Swagger
$swaggerUrl = "http://localhost:8080/swagger/v1/swagger.json"
Write-Host "Aguardando Swagger em: $swaggerUrl (timeout: $WaitSeconds s)"
$sw = $null
$timer = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $sw -and $timer.Elapsed.TotalSeconds -lt $WaitSeconds) {
  try { $sw = Invoke-RestMethod $swaggerUrl -TimeoutSec 5 } catch { Start-Sleep 2 }
}
$timer.Stop()

if ($sw) {
  Write-Host "OK! Swagger respondeu em $([int]$timer.Elapsed.TotalSeconds)s"
  # lista POSTs
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
} else {
  Write-Warning "Swagger não respondeu em $WaitSeconds s."
  Write-Host "Logs: docker compose logs -f barberbook-api"
}
