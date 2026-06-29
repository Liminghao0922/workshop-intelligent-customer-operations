$ErrorActionPreference = "Stop"

Write-Host "Checking prerequisites..."
dotnet --version | Out-Null
python --version | Out-Null

Write-Host "Restoring .NET API dependencies..."
dotnet restore ".\src\api\CustomerOperations.Api\CustomerOperations.Api.csproj"

Write-Host "Installing docs dependencies..."
if (Test-Path ".\.venv\Scripts\pip.exe") {
  .\.venv\Scripts\pip.exe install -r requirements.txt | Out-Null
} else {
  pip install -r requirements.txt | Out-Null
}

Write-Host "Local setup complete."
