@echo off
chcp 65001 >nul
echo [*] Compilando instalador oficial de CleanDesk v1.0...
if not exist "release_build" mkdir "release_build"
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ /win32icon:app.ico /resource:CleanDesk.exe,CleanDesk.exe /resource:CleanDesk.exe.config,CleanDesk.exe.config /resource:app.ico,app.ico /out:release_build\CleanDesk-v1.0-Setup.exe installer\SetupProgram.cs /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll
if %ERRORLEVEL% EQU 0 (
    echo [OK] Instalador generado exitosamente en: release_build\CleanDesk-v1.0-Setup.exe
) else (
    echo [ERROR] Fallo al compilar el instalador.
)
