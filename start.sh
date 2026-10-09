#!/usr/bin/env bash
cd "$(dirname "$0")"
if ! command -v dotnet >/dev/null; then
  echo "Das .NET 10 SDK ist nicht installiert: https://dotnet.microsoft.com/download/dotnet/10.0"
  exit 1
fi
echo "Ticketsystem wird gestartet … (Beenden mit Strg+C)"
dotnet run -c Release --launch-profile Ticketsystem
