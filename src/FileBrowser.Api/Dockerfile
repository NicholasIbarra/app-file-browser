# Build context is the repository root:
#   docker build -f src/FileBrowser.Api/Dockerfile -t file-browser-api .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/FileBrowser.Api/FileBrowser.Api.csproj src/FileBrowser.Api/
COPY src/FileBrowser.Application/FileBrowser.Application.csproj src/FileBrowser.Application/
COPY src/FileBrowser.Infrastructure/FileBrowser.Infrastructure.csproj src/FileBrowser.Infrastructure/
RUN dotnet restore src/FileBrowser.Api/FileBrowser.Api.csproj
COPY src/ src/
RUN dotnet build src/FileBrowser.Api/FileBrowser.Api.csproj -c $BUILD_CONFIGURATION --no-restore -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/FileBrowser.Api/FileBrowser.Api.csproj -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileBrowser.Api.dll"]
