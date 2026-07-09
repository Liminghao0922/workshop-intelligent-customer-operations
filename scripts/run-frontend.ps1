$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot\.."
dotnet run --project ".\src\aspire\IntelligentCustomerOperations.Portal\IntelligentCustomerOperations.Portal.csproj"
