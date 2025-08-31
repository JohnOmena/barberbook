Param([string]$BaseUrl = "http://localhost:8080")

$services = @(
  @{ name="Degrade na zero";                durationMinutes=30 }
  @{ name="Degrade navalhado";              durationMinutes=40 }
  @{ name="Corte social máquina e tesoura"; durationMinutes=20 }
  @{ name="Corte só máquina";               durationMinutes=20 }
  @{ name="Corte na tesoura";               durationMinutes=30 }
  @{ name="Barba";                           durationMinutes=20 }
  @{ name="Pé de cabelo e sobrancelha";     durationMinutes=10 }
  @{ name="Cabelo e barba";                 durationMinutes=50 }
)

foreach ($s in $services) {
  try {
    $resp = Invoke-RestMethod -Method Post `
      -Uri "$BaseUrl/api/services" `
      -ContentType "application/json" `
      -Body ($s | ConvertTo-Json -Depth 5)
    Write-Host "OK: $($s.name)" -ForegroundColor Green
  } catch {
    Write-Host "FAIL: $($s.name) -> $($_.Exception.Message)" -ForegroundColor Red
  }
}
