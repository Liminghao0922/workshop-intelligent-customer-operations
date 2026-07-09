$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot\.."
dotnet run --project ".\src\aspire\IntelligentCustomerOperations.AppHost\IntelligentCustomerOperations.AppHost.csproj"
