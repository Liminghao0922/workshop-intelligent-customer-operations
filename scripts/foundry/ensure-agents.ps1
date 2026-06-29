param(
    [string]$MainAgentName = "smart-call-center-main",
    [string]$AnalyticsAgentName = "smart-call-center-analytics"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-AzdEnvironmentMap {
    $map = @{}
    $lines = azd env get-values

    foreach ($line in $lines) {
        if ($line -match '^([A-Za-z0-9_]+)=(.*)$') {
            $key = $matches[1]
            $value = $matches[2].Trim()
            if ($value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            $map[$key] = $value
        }
    }

    return $map
}

function Ensure-Agent {
    param(
        [string]$Name,
        [string]$Instructions,
        [string]$Model,
        [string]$ListUri,
        [hashtable]$Headers,
        [array]$ExistingAgents
    )

    $existing = $ExistingAgents | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if ($null -ne $existing -and -not [string]::IsNullOrWhiteSpace($existing.id)) {
        Write-Host "Found existing agent '$Name' -> $($existing.id)"
        return $existing.id
    }

    $payload = @{
        name = $Name
        definition = @{
            kind = "Prompt"
            model = $Model
            instructions = $Instructions
        }
    }

    $created = Invoke-RestMethod -Method Post -Uri $ListUri -Headers $Headers -Body ($payload | ConvertTo-Json -Depth 10)
    if ($null -eq $created -or [string]::IsNullOrWhiteSpace($created.id)) {
        throw "Failed to create agent '$Name'."
    }

    Write-Host "Created agent '$Name' -> $($created.id)"
    return $created.id
}

$envMap = Get-AzdEnvironmentMap
$projectEndpoint = $envMap["AZURE_AI_PROJECT_ENDPOINT"]
$apiVersion = if ($envMap.ContainsKey("FOUNDRY_API_VERSION") -and -not [string]::IsNullOrWhiteSpace($envMap["FOUNDRY_API_VERSION"])) { $envMap["FOUNDRY_API_VERSION"] } else { "v1" }
$model = if ($envMap.ContainsKey("FOUNDRY_AGENT_MODEL") -and -not [string]::IsNullOrWhiteSpace($envMap["FOUNDRY_AGENT_MODEL"])) {
    $envMap["FOUNDRY_AGENT_MODEL"]
} elseif ($envMap.ContainsKey("VOICE_LIVE_MODEL") -and -not [string]::IsNullOrWhiteSpace($envMap["VOICE_LIVE_MODEL"])) {
    $envMap["VOICE_LIVE_MODEL"]
} else {
    ""
}

if ([string]::IsNullOrWhiteSpace($projectEndpoint)) {
    Write-Warning "AZURE_AI_PROJECT_ENDPOINT is empty. Skip agent automation. Run 'azd provision' first."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($model)) {
    Write-Warning "FOUNDRY_AGENT_MODEL and VOICE_LIVE_MODEL are both empty. Skip agent automation."
    Write-Warning "Set one of them, then rerun: ./scripts/foundry/ensure-agents.ps1"
    exit 0
}

$baseUri = $projectEndpoint.TrimEnd('/')
$listUri = "$baseUri/agents?api-version=$apiVersion"

function Get-FoundryAccessToken {
    $resources = @(
        "https://ai.azure.com",
        "https://cognitiveservices.azure.com"
    )

    foreach ($resource in $resources) {
        try {
            $candidate = az account get-access-token --resource $resource --query accessToken -o tsv 2>$null
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                Write-Host "Using access token audience: $resource"
                return $candidate
            }
        }
        catch {
            continue
        }
    }

    throw "Failed to acquire Azure access token for Foundry data plane (tried ai.azure.com and cognitiveservices.azure.com)."
}

$token = Get-FoundryAccessToken

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$listResponse = $null
try {
    $listResponse = Invoke-RestMethod -Method Get -Uri $listUri -Headers $headers
}
catch {
    if ($_.Exception.Message -match "UnsupportedApiVersion" -and $apiVersion -ne "v1") {
        Write-Warning "FOUNDRY_API_VERSION '$apiVersion' is not supported by this endpoint. Falling back to 'v1'."
        $apiVersion = "v1"
        $listUri = "$baseUri/agents?api-version=$apiVersion"
        $listResponse = Invoke-RestMethod -Method Get -Uri $listUri -Headers $headers
    }
    else {
        throw
    }
}
$existingAgents = @()
if ($null -ne $listResponse -and $null -ne $listResponse.data) {
    $existingAgents = @($listResponse.data)
}

$mainInstructions = "You are the main voice support agent for Smart Call Center. Respond concisely, ground answers on knowledge, and escalate to human when requested."
$analyticsInstructions = "You are the post-call analytics agent. Summarize the call, detect customer sentiment, and output structured QA findings for follow-up."

$mainAgentId = Ensure-Agent -Name $MainAgentName -Instructions $mainInstructions -Model $model -ListUri $listUri -Headers $headers -ExistingAgents $existingAgents
$analyticsAgentId = Ensure-Agent -Name $AnalyticsAgentName -Instructions $analyticsInstructions -Model $model -ListUri $listUri -Headers $headers -ExistingAgents $existingAgents

azd env set FOUNDRY_AGENT_ID $mainAgentId | Out-Null
azd env set FOUNDRY_ANALYTICS_AGENT_ID $analyticsAgentId | Out-Null

Write-Host "Updated azd environment values:"
Write-Host "  FOUNDRY_AGENT_ID=$mainAgentId"
Write-Host "  FOUNDRY_ANALYTICS_AGENT_ID=$analyticsAgentId"
Write-Host "Done. Run 'azd deploy' to apply updated env vars to Container Apps."
