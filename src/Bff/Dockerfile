# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Bff/*.csproj src/Bff/
RUN dotnet restore src/Bff

COPY src/Bff/ src/Bff/
RUN dotnet publish src/Bff -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD curl -fsS http://127.0.0.1:8080/health || exit 1

USER $APP_UID

ENTRYPOINT ["sh", "-c", "exec dotnet \"$(basename -s .runtimeconfig.json *.runtimeconfig.json).dll\""]
