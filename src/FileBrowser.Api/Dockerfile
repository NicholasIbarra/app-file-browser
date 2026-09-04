# syntax=docker/dockerfile:1
#
# Build context is the repository root (project references reach across
# src/FileBrowser.Application and src/FileBrowser.Infrastructure):
#   docker build -f src/FileBrowser.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/FileBrowser.Api/FileBrowser.Api.csproj src/FileBrowser.Api/
COPY src/FileBrowser.Application/FileBrowser.Application.csproj src/FileBrowser.Application/
COPY src/FileBrowser.Infrastructure/FileBrowser.Infrastructure.csproj src/FileBrowser.Infrastructure/

RUN dotnet restore src/FileBrowser.Api/FileBrowser.Api.csproj

COPY src/ src/

RUN dotnet publish src/FileBrowser.Api/FileBrowser.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FileBrowser.Api.dll"]
