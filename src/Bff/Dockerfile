# ── Stage 1: restore (cached until .csproj / props change) ──────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Bff/Bff.csproj src/Bff/

RUN dotnet restore src/Bff/Bff.csproj

# ── Stage 2: publish ─────────────────────────────────────────────────────────
FROM restore AS publish
COPY src/Bff/ src/Bff/

RUN dotnet publish src/Bff/Bff.csproj \
      -c Release \
      --no-restore \
      -o /app/publish

# ── Stage 3: runtime (small, non-root) ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser

COPY --from=publish --chown=appuser:appgroup /app/publish ./

USER appuser

ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100

ENTRYPOINT ["dotnet", "Omne.Screen.Bff.dll"]
