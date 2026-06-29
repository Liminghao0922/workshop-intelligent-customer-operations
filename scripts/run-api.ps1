$ErrorActionPreference = "Stop"
Set-Location "$PSScriptRoot\.."
dotnet run --project ".\src\api\CustomerOperations.Api\CustomerOperations.Api.csproj"
