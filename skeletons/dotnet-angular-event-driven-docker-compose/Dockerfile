# One runtime image for the api and every worker; services differ only in the
# command (adrs/deployment/docker-multi-stage-builds.md, adrs/deployment/container-per-process.md)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY . .
RUN dotnet publish App.Api -c Release -o /publish/api \
 && dotnet publish Orders.Worker -c Release -o /publish/workers/orders \
 && dotnet publish Outbox.Dispatch -c Release -o /publish/workers/outbox

# runtime-only base image, never the SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /srv
COPY --from=build /publish .
EXPOSE 8080
CMD ["dotnet", "/srv/api/App.Api.dll"]
