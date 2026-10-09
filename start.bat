@echo off
chcp 65001 >nul
title Ticketsystem
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo  Das .NET SDK ist nicht installiert.
    echo  Bitte .NET 10 SDK installieren:  https://dotnet.microsoft.com/download/dotnet/10.0
    echo  oder per Kommandozeile:          winget install Microsoft.DotNet.SDK.10
    echo.
    pause
    exit /b 1
)

echo.
echo  Ticketsystem wird gestartet ...  (beim ersten Mal dauert es etwas)
echo  Der Browser oeffnet sich automatisch. Zum Beenden dieses Fenster schliessen.
echo.
dotnet run -c Release --launch-profile Ticketsystem
if errorlevel 1 pause
