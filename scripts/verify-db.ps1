# C:\dev\barberbook\scripts\verify-db.ps1
Param([string]$Root)

# Descobre a raiz do projeto
if (-not $Root) {
  if ($env:BARBERBOOK_ROOT) { $Root = $env:BARBERBOOK_ROOT }
  elseif ($PSScriptRoot)    { $Root = (Resolve-Path "$PSScriptRoot\..").Path }
  else                      { $Root = (Resolve-Path "C:\dev\barberbook").Path }
}

$compose = Join-Path $Root "docker-compose.yml"

# Garante Postgres rodando
$pgCid = (docker compose -f $compose ps -q postgres).Trim()
if (-not $pgCid) {
  Write-Error "Container 'postgres' não está rodando. Execute: docker compose up -d"
  exit 1
}

# SQL de verificação (atenção às aspas nas colunas com maiúsculas)
$verifySql = @'
SELECT "Id","Name" FROM tenants;
SELECT "Name","IsDefault" FROM professionals;
SELECT "Name","DurationMin" FROM services ORDER BY "Name";
SELECT "Weekday","Start","End" FROM availabilities ORDER BY 1,2;
'@

# Escreve e executa
$local = Join-Path $Root "scripts\verify.sql"
$verifySql | Set-Content -Encoding utf8 $local

docker cp $local "$pgCid`:/tmp/verify.sql" | Out-Null
docker compose -f $compose exec -T postgres psql -U barber -d barberbook -f /tmp/verify.sql
