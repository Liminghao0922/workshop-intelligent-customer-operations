param(
  [string]$GatewayBaseUrl = "http://localhost:61989"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking Gateway health..."
$health = Invoke-RestMethod -Method Get -Uri "$GatewayBaseUrl/healthz"
$health | ConvertTo-Json -Depth 5

$languages = @(
  "en",
  "ja",
  "zh"
)

$results = @()
foreach ($language in $languages) {
  Write-Host "`nSimulating call in language: $language"
  $body = @{
    language = $language
  } | ConvertTo-Json

  $resp = Invoke-RestMethod -Method Post -Uri "$GatewayBaseUrl/api/dev/simulate-call" -ContentType "application/json" -Body $body
  $resp | ConvertTo-Json -Depth 5
  $results += $resp
}

$calls = Invoke-RestMethod -Method Get -Uri "$GatewayBaseUrl/api/calls"

$outDir = Join-Path $PSScriptRoot "..\assets\screenshots"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outPath = Join-Path $outDir "demo-output.json"
@{
  simulated = $results
  totalCalls = @($calls).Count
  latestCalls = @($calls) | Select-Object -First 5
} | ConvertTo-Json -Depth 12 | Out-File -Encoding UTF8 $outPath
Write-Host "`nDemo output saved to $outPath"
