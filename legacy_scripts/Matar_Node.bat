@echo off
chcp 65001 >nul
title Cerrar Procesos Node.js
color 0C

echo =======================================================
echo          MATAR PROCESOS NODE.JS Y DERIVADOS
echo =======================================================
echo.

:: Verificar si existen procesos node.exe activos
tasklist /FI "IMAGENAME eq node.exe" 2>nul | find /I /N "node.exe" >nul
if "%ERRORLEVEL%"=="0" (
    echo [!] Se detectaron procesos node.exe en ejecucion.
    echo [*] Finalizando todos los procesos y su arbol de ejecucion...
    echo.
    taskkill /F /IM node.exe /T
    echo.
    color 0A
    echo [OK] Todos los procesos node.exe fueron cerrados exitosamente.
) else (
    color 0E
    echo [i] No se encontraron procesos node.exe en ejecucion actualmente.
)

:: Asegurar limpieza de procesos huerfanos con PowerShell
powershell -NoProfile -Command "Stop-Process -Name 'node' -Force -ErrorAction SilentlyContinue" >nul 2>&1

echo.
echo =======================================================
echo Cerrando esta ventana en 4 segundos... (o presiona una tecla)
timeout /t 4 >nul
