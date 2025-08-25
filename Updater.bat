@echo off
setlocal EnableExtensions

REM Dossiers
set "ROOT=%~dp0"
set "OUT=%ROOT%build"


REM Nettoyer la sortie SANS supprimer les zips source
del /Q /F "%ROOT%\*.exe" 2>nul
del /Q /F "%ROOT%\*.zip" 2>nul

REM 2) Extraire les zip de build
for %%f in ("%OUT%\*.zip") do (
    echo Décompression de "%%~nxf" dans "%OUT%"
    tar -xf "%%~ff" -C "%OUT%"
)



echo Terminé.
exit /b 0
