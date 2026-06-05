# syntax=docker/dockerfile:1
# Build context: repo root

# ── restore ────────────────────────────────────────────────────────────────────
# Copy only manifests first so the restore layer is cache-stable across
# source-only changes.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj

# ── publish ────────────────────────────────────────────────────────────────────
FROM restore AS publish
COPY src/Api/ src/Api/
RUN dotnet publish src/Api/Api.csproj \
        -c Release \
        --no-restore \
        -o /app/publish

# ── runtime ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=publish --chown=app:app /app/publish .

USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Omne.Screen.Api.dll"]
