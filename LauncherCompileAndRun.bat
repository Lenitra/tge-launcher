@echo off
echo Compilation du projet en mode portable...

:: Définir la version du projet à compiler : yyyymmddhhMMss
set FILENAMEE=%date:~6,4%%date:~3,2%%date:~0,2%%time:~0,2%%time:~3,2%%time:~6,2%
set FILENAMEE=%FILENAMEE: =0%
echo Version du projet : %FILENAMEE%



:: Ajout d'un dossier de build s'il n'existe pas
if not exist ".\build" mkdir ".\build"

:: Supprimer les anciens fichiers de build pour qu'ils en reste maximum 1
for /f "skip=1 delims=" %%i in ('dir /b /ad /o-d .\build') do (
    echo Suppression du dossier : %%i
    rmdir /s /q ".\build\%%i"
)

:: Ajout d'un dossier ayant le nom de la version dans build s'il n'existe pas
if not exist ".\build\v%FILENAMEE%" mkdir ".\build\v%FILENAMEE%"


:: Copier TrucksBook dans le dossier de publication
if not exist ".\build\v%FILENAMEE%\TrucksBook" mkdir ".\build\v%FILENAMEE%\TrucksBook"
xcopy /E /I /Y ".\TrucksBook\*" ".\build\v%FILENAMEE%\TrucksBook\"

:: Copier le fichier Updater.bat
xcopy /I /Y ".\LauncherSrc\tomove\Updater.bat" ".\build\v%FILENAMEE%\"

:: Compiler le mod
powershell -Command "Compress-Archive -Path 'TGE-Mod' -DestinationPath 'build\v%FILENAMEE%\tge_mod_%FILENAMEE%.zip'"


echo {"Email": "","Password": ""} > ".\build\v%FILENAMEE%\credentials.json"

:: Compilation en mode self-contained pour Windows
dotnet publish .\LauncherSrc\LauncherSrc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ".\build\v%FILENAMEE%" 
echo Compilation terminée ! L'application portable est dans le dossier "build\v%FILENAMEE%".


:: Faire un zip du dossier build\v%FILENAMEE%
powershell -Command "Compress-Archive -Path '.\build\v%FILENAMEE%\*' -DestinationPath '.\build\v%FILENAMEE%\LauncherTGE_%FILENAMEE%.zip' -CompressionLevel Optimal"

:: Executer le exe généré
echo Démarrage de l'application...
cd ".\build\v%FILENAMEE%"

@echo on
start "" ".\LauncherSrc.exe"
