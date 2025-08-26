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

REM Déplacer les fichier du dossier build en .exe et .pdb et dans le répertoire ROOT
for %%f in ("%OUT%\*.exe" "%OUT%\*.pdb" "%OUT%\*.bat" "%OUT%\*.zip") do (
    echo Déplacement de "%%~nxf" vers "%ROOT%"
    move /Y "%%~ff" "%ROOT%"
)

REM Déplacer le zip du mod qui à pour regex : tge_mod_([0-9])
for %%f in ("%ROOT%\tge_mod_([0-9]+).zip") do (
    echo Déplacement de "%%~nxf" vers "%ROOT%"
    move /Y "%%~ff" "%ROOT%"
)

REM Supprimer le dossier build
rmdir /S /Q "%OUT%"

echo Terminé.

REM Lancer l'application
call start "" "%ROOT%\LauncherSrc.exe"

REM Fermer la console
exit 
