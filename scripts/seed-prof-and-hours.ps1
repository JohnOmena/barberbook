Param(
  [string]$TenantName = "default",
  [string]$ProfName   = "Barbeiro"
)

# Gera UUIDs determinísticos por nome (para rodar N vezes sem duplicar)
function New-DeterministicGuid([string]$seed) {
  # hash sha1 -> formata como guid v4-like (suficiente para nossa finalidade local)
  $h = [System.BitConverter]::ToString((New-Object System.Security.Cryptography.SHA1Managed).ComputeHash([Text.Encoding]::UTF8.GetBytes($seed))).Replace("-","").ToLower()
  return "{0}-{1}-{2}-{3}-{4}" -f $h.Substring(0,8),$h.Substring(8,4),$h.Substring(12,4),$h.Substring(16,4),$h.Substring(20,12)
}

$tenantId = New-DeterministicGuid("tenant:"+$TenantName)
$profId   = New-DeterministicGuid("prof:"+$ProfName)

# Segunda-feira(1) a Sábado(6) 09:00-18:00
$week = 1..6 | ForEach-Object { @{ weekday = $_; start = "09:00"; ["end"] = "18:00" } }

$sql = @"
-- Tenant
INSERT INTO tenants (id, name)
SELECT '$tenantId'::uuid, '$TenantName'
WHERE NOT EXISTS (SELECT 1 FROM tenants WHERE name = '$TenantName');

-- Profissional default
INSERT INTO professionals (id, "Name", "Active", "IsDefault", "TenantId")
SELECT '$profId'::uuid, '$ProfName', true, true, '$tenantId'::uuid
WHERE NOT EXISTS (
  SELECT 1 FROM professionals WHERE "TenantId" = '$tenantId'::uuid AND "IsDefault" = true
);

-- Disponibilidades (seg-sáb, 09-18) se ainda não existirem
-- Ajuste se sua tabela/colunas tiverem nomes/casos diferentes
"@

foreach ($d in $week) {
  $wid   = New-DeterministicGuid("av:$($d.weekday)")
  $w     = $d.weekday
  $start = $d.start
  $end   = $d.end
  $sql += @"
INSERT INTO availabilities (id, "TenantId", "ProfessionalId", "Weekday", "Start", "End")
SELECT '$wid'::uuid, '$tenantId'::uuid, '$profId'::uuid, $w, '$start'::time, '$end'::time
WHERE NOT EXISTS (
  SELECT 1 FROM availabilities
  WHERE "TenantId" = '$tenantId'::uuid AND "ProfessionalId" = '$profId'::uuid
    AND "Weekday" = $w AND "Start" = '$start'::time AND "End" = '$end'::time
);
"@
}

# Executa no container Postgres
$cmd = @(
  "docker","compose","exec","-T","postgres",
  "psql","-U","barber","-d","barberbook","-v","ON_ERROR_STOP=1","-c",$sql
)
Write-Host "Aplicando seed no Postgres..." -ForegroundColor Cyan
$proc = Start-Process -NoNewWindow -PassThru -FilePath $cmd[0] -ArgumentList $cmd[1..($cmd.Count-1)]
$proc.WaitForExit()
if ($proc.ExitCode -eq 0) {
  Write-Host "Seed OK." -ForegroundColor Green
} else {
  Write-Host "Seed falhou (ExitCode=$($proc.ExitCode)). Confira o schema/nomes das colunas." -ForegroundColor Red
}
