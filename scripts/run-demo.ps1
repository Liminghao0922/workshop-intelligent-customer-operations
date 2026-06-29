param(
  [string]$ApiBaseUrl = "http://localhost:5077"
)

$ErrorActionPreference = "Stop"

Write-Host "Checking API health..."
$health = Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/health"
$health | ConvertTo-Json -Depth 5

$prompts = @(
  "What information do I need to check product warranty?",
  "Can you check the status of service request SR-1001?",
  "Can you check my repair status?",
  "This is the third time the same issue happened and I need escalation."
)

$results = @()
foreach ($prompt in $prompts) {
  Write-Host "`nPrompt: $prompt"
  $body = @{
    message = $prompt
    sessionId = "demo-session-001"
  } | ConvertTo-Json

  $resp = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/chat" -ContentType "application/json" -Body $body
  $resp | ConvertTo-Json -Depth 5
  $results += $resp
}

$outDir = Join-Path $PSScriptRoot "..\assets\screenshots"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outPath = Join-Path $outDir "demo-output.json"
$results | ConvertTo-Json -Depth 10 | Out-File -Encoding UTF8 $outPath
Write-Host "`nDemo output saved to $outPath"
