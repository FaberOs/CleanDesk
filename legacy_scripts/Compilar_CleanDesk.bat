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
if exist "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\app.ico" (
    echo [OK] Usando app.ico personalizado del usuario.
    if not exist "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\bin\Release" mkdir "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\bin\Release"
    copy /Y "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\app.ico" "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\bin\Release\app.ico" >nul 2>&1
) else (
    echo [*] Generando icono inicial...
    powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\generate_icon.ps1"
)

echo [*] Compilando con MSBuild nativo .NET 4.0/4.5...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\CleanDeskWidget.csproj" /p:Configuration=Release /p:Platform=x64 /nologo

if %ERRORLEVEL% NEQ 0 (
    color 0C
    echo.
    echo [ERROR] La compilacion ha fallado. Revisa los mensajes arriba.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [*] Desplegando ejecutable actualizado...
copy /Y "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\bin\Release\CleanDesk.exe" "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\CleanDesk.exe" >nul

echo.
color 0A
echo [OK] Compilacion y despliegue exitoso!
echo [*] Iniciando CleanDesk Widget...
start "" "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget\CleanDesk.exe"

timeout /t 3 >nul
