@echo off
chcp 65001 >nul
title Compilando CleanDesk Widget...
color 0B

echo =======================================================
echo          COMPILANDO CLEANDESK WIDGET (RELEASE X64)
echo =======================================================
echo.

echo [*] Cerrando instancias previas de CleanDesk...
taskkill /F /IM CleanDesk.exe 2>nul

echo [*] Sincronizando icono app.ico personalizado...
if exist "%~dp0app.ico" (
    echo [OK] Usando app.ico personalizado del usuario.
    if not exist "%~dp0bin\Release" mkdir "%~dp0bin\Release"
    copy /Y "%~dp0app.ico" "%~dp0bin\Release\app.ico" >nul 2>&1
) else (
    echo [*] Generando icono inicial...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generate_icon.ps1"
)

echo [*] Compilando con MSBuild nativo .NET 4.0/4.5...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "%~dp0CleanDeskWidget.csproj" /p:Configuration=Release /p:Platform=x64 /nologo

if %ERRORLEVEL% NEQ 0 (
    color 0C
    echo.
    echo [ERROR] La compilacion ha fallado. Revisa los mensajes arriba.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [*] Desplegando ejecutable en la raiz del proyecto...
copy /Y "%~dp0bin\Release\CleanDesk.exe" "%~dp0CleanDesk.exe" >nul

echo.
color 0A
echo [OK] Compilacion y despliegue exitoso!
echo [*] Iniciando CleanDesk Widget...
start "" "%~dp0CleanDesk.exe"

timeout /t 3 >nul
