FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY IntelligentCustomerOperations.ApiService.csproj .
RUN dotnet restore IntelligentCustomerOperations.ApiService.csproj
COPY . .
RUN dotnet publish IntelligentCustomerOperations.ApiService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "IntelligentCustomerOperations.ApiService.dll"]

