param(
    [string]$GatewayBaseUrl = "http://localhost:61989",
    [string]$Language = "en"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$base = $GatewayBaseUrl.TrimEnd('/')

Write-Host "Checking gateway health..."
$health = Invoke-RestMethod -Method Get -Uri "$base/healthz"
Assert-True ($health.status -eq "ok") "Gateway health check failed."

Write-Host "Checking gateway config..."
$config = Invoke-RestMethod -Method Get -Uri "$base/api/config"
Assert-True ($config.mode -eq "azure") "APP_MODE is '$($config.mode)'. Expected 'azure'."
Assert-True ([bool]$config.foundryConfigured) "Foundry is not configured."
Assert-True ([bool]$config.searchConfigured) "Search is not configured."
Assert-True ([bool]$config.storageConfigured) "Storage is not configured."

Write-Host "Simulating call in language '$Language'..."
$call = Invoke-RestMethod -Method Post -Uri "$base/api/dev/simulate-call" -ContentType "application/json" -Body (@{ language = $Language } | ConvertTo-Json)
Assert-True (-not [string]::IsNullOrWhiteSpace($call.id)) "Simulated call did not return call id."

$callId = [string]$call.id
Write-Host "Fetching call details: $callId"
$callDetails = Invoke-RestMethod -Method Get -Uri "$base/api/calls/$callId"

Assert-True (($callDetails.transcript | Measure-Object).Count -ge 2) "Transcript is too short. Expected customer + assistant turns."
Assert-True (-not [string]::IsNullOrWhiteSpace($callDetails.foundryConversationId)) "Foundry conversation mapping missing."

$artifactTypes = @($callDetails.artifacts | ForEach-Object { $_.type })
Assert-True (-not ($artifactTypes -contains "assistant_reply_error")) "Foundry reply failed (assistant_reply_error)."
Assert-True (-not ($artifactTypes -contains "assistant_reply_timeout")) "Foundry reply timeout detected."

Write-Host "Triggering post-call analysis..."
$analyzeResult = Invoke-RestMethod -Method Post -Uri "$base/api/admin/analyze/$callId"
Assert-True ($null -ne $analyzeResult) "Analyze endpoint returned empty response."

$postAnalyze = Invoke-RestMethod -Method Get -Uri "$base/api/calls/$callId"
Assert-True ($postAnalyze.analyticsStatus -eq "submitted") "analyticsStatus is '$($postAnalyze.analyticsStatus)'. Expected 'submitted'."
Assert-True ($null -ne $postAnalyze.postCallResult) "postCallResult missing after analyze."

Write-Host "Azure mode validation passed. callId=$callId"
