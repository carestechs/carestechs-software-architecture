# One image serves API + SPA (adrs/deployment/spa-served-by-api.md,
# adrs/deployment/docker-multi-stage-builds.md)

# Stage 1: Angular production bundle
FROM node:22-alpine AS client
WORKDIR /client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client .
RUN npm run build

# Stage 2: publish the web app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY Directory.Build.props BannedSymbols.txt .editorconfig ./
COPY src ./src
RUN dotnet publish src/MyApp.Web -c Release -o /publish

# Stage 3: runtime-only image; the SPA bundle replaces the wwwroot placeholder
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /srv
COPY --from=build /publish .
COPY --from=client /client/dist/client/browser ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApp.Web.dll"]
