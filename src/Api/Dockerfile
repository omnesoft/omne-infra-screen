# syntax=docker/dockerfile:1
# Build context: repo root

# ── restore ────────────────────────────────────────────────────────────────────
# Copy only manifests first so the restore layer is cache-stable across
# source-only changes.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS restore
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711 AS runtime
WORKDIR /app

# curl is required by the container HEALTHCHECK; the aspnet image ships neither
# curl nor wget. Installed as root before dropping to the non-root app user.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish --chown=app:app /app/publish .

USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Omne.Screen.Api.dll"]
