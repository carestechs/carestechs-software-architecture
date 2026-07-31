# Multi-stage backend build (adrs/deployment/docker-multi-stage-builds.md)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY Directory.Build.props BannedSymbols.txt .editorconfig ./
COPY src ./src
RUN dotnet publish src/MyApp.Api -c Release -o /publish

# runtime-only base image, never the SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /srv
COPY --from=build /publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
