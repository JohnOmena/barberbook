Param(
  [string]$TenantName = "default",
  [string]$ProfName   = "Barbeiro"
)

# --- Helpers ---
function New-DeterministicGuid([string]$seed) {
  $sha1 = New-Object System.Security.Cryptography.SHA1Managed
  $h = [System.BitConverter]::ToString($sha1.ComputeHash([Text.Encoding]::UTF8.GetBytes($seed))).Replace("-","").ToLower()
  "{0}-{1}-{2}-{3}-{4}" -f $h.Substring(0,8),$h.Substring(8,4),$h.Substring(12,4),$h.Substring(16,4),$h.Substring(20,12)
}
function ToSlug([string]$s){ ($s -replace '\s+','-').ToLower() -replace '[^a-z0-9\-]','' }

$tenantId = New-DeterministicGuid("tenant:"+$TenantName)
$profId   = New-DeterministicGuid("prof:"+$ProfName)

$week = 1..6 | ForEach-Object { @{ weekday = $_; start = '09:00'; 'end' = '18:00' } }

$services = @(
  @{ name='Degrade na zero';                dur=30 }
  @{ name='Degrade navalhado';              dur=40 }
  @{ name='Corte social máquina e tesoura'; dur=20 }
  @{ name='Corte só máquina';               dur=20 }
  @{ name='Corte na tesoura';               dur=30 }
  @{ name='Barba';                           dur=20 }
  @{ name='Pé de cabelo e sobrancelha';     dur=10 }
  @{ name='Cabelo e barba';                 dur=50 }
) | ForEach-Object {
  $_ + @{ id = (New-DeterministicGuid("svc:"+$_.name)); slug = (ToSlug $_.name) }
}

# --- Monta SQL idempotente com identificadores entre aspas ---
$sql = @"
-- Tenant
INSERT INTO tenants ("Id","Name")
SELECT '$tenantId'::uuid, '$TenantName'
WHERE NOT EXISTS (SELECT 1 FROM tenants WHERE "Id"='$tenantId'::uuid OR "Name"='$TenantName');

-- Profissional default
INSERT INTO professionals ("Id","Name","Active","IsDefault","TenantId")
SELECT '$profId'::uuid, '$ProfName', true, true, '$tenantId'::uuid
WHERE NOT EXISTS (SELECT 1 FROM professionals WHERE "TenantId"='$tenantId'::uuid AND "IsDefault"=true);
"@

foreach ($d in $week) {
  $wid = New-DeterministicGuid("av:$($d.weekday)")
  $w   = $d.weekday
  $st  = $d.start
  $en  = $d.'end'
  $sql += @"
INSERT INTO availabilities ("Id","TenantId","ProfessionalId","Weekday","Start","End")
SELECT '$wid'::uuid, '$tenantId'::uuid, '$profId'::uuid, $w, '$st'::time, '$en'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId"='$tenantId'::uuid AND "ProfessionalId"='$profId'::uuid
    AND "Weekday"=$w AND "Start"='$st'::time AND "End"='$en'::time
);
"@
}

foreach ($s in $services) {
  $id   = $s.id
  $name = $s.name.Replace("'", "''")
  $dur  = [int]$s.dur
  $slug = $s.slug
  $sql += @"
INSERT INTO services ("Id","TenantId","Name","DurationMin","Active","BufferMin","Price","Slug")
SELECT '$id'::uuid, '$tenantId'::uuid, '$name', $dur, true, 0, 0, '$slug'
WHERE NOT EXISTS (
  SELECT 1 FROM services WHERE "TenantId"='$tenantId'::uuid AND "Name"='$name'
);
"@
}

# --- Escreve arquivo .sql local e copia para o container ---
$root = "C:\dev\barberbook"
$localSql = Join-Path $root "scripts\seed-all.sql"
$sql | Set-Content -Encoding utf8 $localSql

$pgCid = (docker compose -f (Join-Path $root "docker-compose.yml") ps -q postgres).Trim()
if (-not $pgCid) {
  Write-Error "Container 'postgres' não encontrado. Rode 'docker compose up -d' na raiz do projeto."
  exit 1
}

# copia para /tmp/ dentro do container
docker cp $localSql "$pgCid`:/tmp/seed-all.sql" | Out-Null

# executa com psql -f (sem problemas de quoting)
docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
  psql -U barber -d barberbook -v ON_ERROR_STOP=1 -f /tmp/seed-all.sql

# --- Verificações rápidas (cuidado com nomes entre aspas) ---
docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
  psql -U barber -d barberbook -c 'SELECT "Id","Name" FROM tenants;'

docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
  psql -U barber -d barberbook -c 'SELECT "Name","IsDefault" FROM professionals;'

docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
  psql -U barber -d barberbook -c 'SELECT "Name","DurationMin" FROM services ORDER BY "Name";'

docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
  psql -U barber -d barberbook -c 'SELECT "Weekday","Start","End" FROM availabilities ORDER BY 1,2;'
