[CmdletBinding()]
param(
    [switch]$Rebuild,
    [int]$TimeoutSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-BarberBookRoot {
    if ($env:BARBERBOOK_ROOT) {
        return (Resolve-Path $env:BARBERBOOK_ROOT).Path
    }
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    return (Resolve-Path (Join-Path (Get-Location).Path '..')).Path
}

function Get-EnvValue {
    param(
        [string[]]$Lines,
        [string]$Key,
        [string]$Default
    )

    $pattern = "^\s*$Key\s*="
    $line = $Lines | Where-Object { $_ -match $pattern } | Select-Object -First 1
    if (-not $line) { return $Default }

    $value = $line.Substring($line.IndexOf('=') + 1)
    $value = $value.Split('#')[0].Trim()
    if (-not $value) { return $Default }

    if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
    return $value
}

function Resolve-ChromePath {
    $candidates = @()
    $chromeCmd = Get-Command 'chrome' -ErrorAction SilentlyContinue
    if ($chromeCmd) { $candidates += $chromeCmd.Source }

    if ($env:ProgramFiles) {
        $candidates += (Join-Path $env:ProgramFiles 'Google/Chrome/Application/chrome.exe')
    }
    if (${env:ProgramFiles(x86)}) {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Google/Chrome/Application/chrome.exe')
    }
    if ($env:LOCALAPPDATA) {
        $candidates += (Join-Path $env:LOCALAPPDATA 'Google/Chrome/Application/chrome.exe')
    }

    foreach ($candidate in ($candidates | Where-Object { $_ })) {
        $candidatePath = $candidate -replace '/', '\\'
        if (Test-Path $candidatePath) { return $candidatePath }
    }

    return $null
}

function Get-ComposeArgs {
    param(
        [string[]]$Files,
        [string]$EnvPath,
        [string[]]$ExtraArgs
    )

    $args = @()
    foreach ($file in $Files) {
        $args += @('-f', $file)
    }
    $args += @('--env-file', $EnvPath)
    if ($ExtraArgs) { $args += $ExtraArgs }
    return $args
}

function Wait-HttpUrl {
    param(
        [string]$Url,
        [int]$TimeoutSeconds,
        [string]$DisplayName
    )

    Write-Host "Aguardando $DisplayName em: $Url (timeout: $TimeoutSeconds s)"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        try {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                $stopwatch.Stop()
                Write-Host "$DisplayName disponivel!" -ForegroundColor Green
                return $true
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    }
    $stopwatch.Stop()
    Write-Warning "$DisplayName nao respondeu dentro do limite de tempo."
    return $false
}

$root = Resolve-BarberBookRoot
$composePath = Join-Path $root 'docker-compose.yml'
$overridePath = Join-Path $root 'docker-compose.override.yml'
$envPath = Join-Path $root '.env'

if (-not (Test-Path $composePath)) {
    throw "docker-compose.yml not found in $root"
}

if (-not (Test-Path $envPath)) {
    Write-Error 'Missing .env. Run scripts\ensure-env.ps1 and configure variables.'
    exit 1
}

$composeFiles = @($composePath)
if (Test-Path $overridePath) {
    $composeFiles += $overridePath
}

$envLines = Get-Content $envPath -ErrorAction Stop

Write-Host 'Starting containers with docker compose...' -ForegroundColor Cyan
$upArgs = Get-ComposeArgs -Files $composeFiles -EnvPath $envPath -ExtraArgs @('up', '-d')
if ($Rebuild) { $upArgs += '--build' }
docker compose @upArgs

$psArgs = Get-ComposeArgs -Files $composeFiles -EnvPath $envPath -ExtraArgs @('ps')
docker compose @psArgs

$defaults = @{
    API_HOST_PORT = '8080'
    WEB_HOST_PORT = '5161'
    WAHA_HOST_PORT = '3000'
    N8N_HOST_PORT = '5678'
}

$ports = @{}
foreach ($key in $defaults.Keys) {
    $ports[$key] = Get-EnvValue -Lines $envLines -Key $key -Default $defaults[$key]
}

$targets = @(
    [pscustomobject]@{ Url = "http://localhost:$($ports.API_HOST_PORT)/swagger/v1/swagger.json"; Name = 'Swagger (JSON)'; OpenUrl = "http://localhost:$($ports.API_HOST_PORT)/swagger/index.html" },
    [pscustomobject]@{ Url = "http://localhost:$($ports.WEB_HOST_PORT)/admin"; Name = 'Painel Web'; OpenUrl = "http://localhost:$($ports.WEB_HOST_PORT)/admin" },
    [pscustomobject]@{ Url = "http://localhost:$($ports.WAHA_HOST_PORT)/"; Name = 'WAHA'; OpenUrl = "http://localhost:$($ports.WAHA_HOST_PORT)/" },
    [pscustomobject]@{ Url = "http://localhost:$($ports.N8N_HOST_PORT)/"; Name = 'n8n'; OpenUrl = "http://localhost:$($ports.N8N_HOST_PORT)/" }
)

$availableUrls = @()
foreach ($target in $targets) {
    if (Wait-HttpUrl -Url $target.Url -TimeoutSeconds $TimeoutSeconds -DisplayName $target.Name) {
        $availableUrls += $target.OpenUrl
    } else {
        # Still add so user can try manually if it came up late
        $availableUrls += $target.OpenUrl
    }
}

Write-Host "`nAbrindo URLs no Chrome:" -ForegroundColor Cyan
$availableUrls | ForEach-Object { Write-Host " - $_" }

$chromePath = Resolve-ChromePath
if ($chromePath) {
    if ($availableUrls.Count -gt 0) {
        $args = @('--new-window', $availableUrls[0])
        for ($i = 1; $i -lt $availableUrls.Count; $i++) {
            $args += @('--new-tab', $availableUrls[$i])
        }
        Start-Process -FilePath $chromePath -ArgumentList $args | Out-Null
    } else {
        Write-Warning 'No URLs to open.'
    }
} else {
    Write-Warning 'Could not locate Google Chrome automatically. Open the URLs manually if needed.'
}
