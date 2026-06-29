FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY IntelligentCustomerOperations.PostCallWorker.csproj .
RUN dotnet restore IntelligentCustomerOperations.PostCallWorker.csproj
COPY . .
RUN dotnet publish IntelligentCustomerOperations.PostCallWorker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

