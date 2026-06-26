# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Api/*.csproj src/Api/
RUN dotnet restore src/Api

COPY src/Api/ src/Api/
RUN dotnet publish src/Api -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["sh", "-c", "exec dotnet \"$(basename -s .runtimeconfig.json *.runtimeconfig.json).dll\""]
