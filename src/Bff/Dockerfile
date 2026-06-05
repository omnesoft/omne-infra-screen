# syntax=docker/dockerfile:1
# Build context: repo root

# ── restore ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=publish --chown=app:app /app/publish .

USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ReverseProxy__Clusters__api-cluster__Destinations__api__Address=http://api:8080/

ENTRYPOINT ["dotnet", "Omne.Screen.Bff.dll"]
