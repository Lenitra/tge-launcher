@echo off
echo Compilation du projet en mode portable...

:: Définir la version du projet à compiler : yyyymmddhhMMss
set FILENAMEE=version%date:~6,4%%date:~3,2%%date:~0,2%%time:~0,2%%time:~3,2%%time:~6,2%
echo Version du projet : %FILENAMEE%



:: Ajout d'un dossier de build s'il n'existe pas
if not exist "..\build" mkdir "..\build"

:: Supprimer les anciens fichiers de build pour qu'ils en reste maximum 4
for /f "skip=4 delims=" %%i in ('dir /b /ad /o-d ..\build') do (
    echo Suppression du dossier : %%i
    rmdir /s /q "..\build\%%i"
)



:: Ajout d'un dossier ayant le nom de la version dans build s'il n'existe pas
if not exist "..\build\%FILENAMEE%" mkdir "..\build\%FILENAMEE%"

:: Copier TrucksBook dans le dossier de publication
if not exist "..\build\%FILENAMEE%\TrucksBook" mkdir "..\build\%FILENAMEE%\TrucksBook"
xcopy /E /I /Y "..\TrucksBook\*" "..\build\%FILENAMEE%\TrucksBook\"


:: Compilation en mode self-contained pour Windows
dotnet publish .\LauncherSrc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "..\build\%FILENAMEE%" 
echo Compilation terminée ! L'application portable est dans le dossier "build\%FILENAMEE%".

:: Executer le exe généré
echo Démarrage de l'application...
cd "..\build\%FILENAMEE%"

@echo on
start "" ".\LauncherSrc.exe"
