# syntax=docker/dockerfile:1
# Build context: repo root

# ── restore ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS restore
WORKDIR /src
COPY nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Bff/Bff.csproj src/Bff/
RUN dotnet restore src/Bff/Bff.csproj

# ── publish ────────────────────────────────────────────────────────────────────
FROM restore AS publish
COPY src/Bff/ src/Bff/
RUN dotnet publish src/Bff/Bff.csproj \
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
    ASPNETCORE_ENVIRONMENT=Production \
    ReverseProxy__Clusters__api-cluster__Destinations__api__Address=http://api:8080/

ENTRYPOINT ["dotnet", "Omne.Screen.Bff.dll"]
