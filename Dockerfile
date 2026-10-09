# ── 1. Bauen ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── 2. Laufzeit ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Deutsche Zeitzone (sonst laufen alle Zeiten auf UTC = 2 Std. daneben),
# Port 8080 im Container, kein Browser-Start
ENV TZ=Europe/Berlin \
    Urls=http://+:8080 \
    BrowserOeffnen=false

# Datenordner für die SQLite-Datenbank (gehört dem App-Benutzer)
USER root
RUN mkdir -p /app/Daten && chown -R $APP_UID /app/Daten
USER $APP_UID

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ticketsystem.dll"]
