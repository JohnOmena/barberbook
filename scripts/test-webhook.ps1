# test-webhook.ps1
# Simula uma mensagem no webhook do n8n
Param([string]$Url = "http://localhost:5678/webhook/wa-incoming")
$body = @{
  body = @{
    from   = "5599999999999@c.us"
    text   = "agendar corte social 2025-09-02"
    fromMe = $false
    me     = @{ pushName = "Joao" }
    id     = "MSG-1"
  }
} | ConvertTo-Json -Depth 10
Invoke-RestMethod -Method Post -Uri $Url -ContentType "application/json" -Body $body
Write-Host "POST enviado para $Url" -ForegroundColor Green
