# ── Stage 1: restore (cached until .csproj / props change) ──────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy only the files that affect restore so this layer is cache-stable
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Api/Api.csproj src/Api/
COPY test/Api.Tests/Api.Tests.csproj test/Api.Tests/

RUN dotnet restore src/Api/Api.csproj

# ── Stage 2: publish ─────────────────────────────────────────────────────────
FROM restore AS publish
COPY src/Api/ src/Api/

RUN dotnet publish src/Api/Api.csproj \
      -c Release \
      --no-restore \
      -o /app/publish

# ── Stage 3: runtime (small, non-root) ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

COPY --from=publish --chown=appuser:appgroup /app/publish ./

USER appuser

ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080

ENTRYPOINT ["dotnet", "Omne.Screen.Api.dll"]
