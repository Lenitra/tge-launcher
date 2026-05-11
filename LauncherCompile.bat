@echo off
setlocal
echo Compilation du projet en mode portable...

set FILENAMEE=%date:~6,4%%date:~3,2%%date:~0,2%%time:~0,2%%time:~3,2%%time:~6,2%
set FILENAMEE=%FILENAMEE: =0%
echo Version : %FILENAMEE%
set BUILDDIR=.\build\v%FILENAMEE%

:: --- Setup dossier de build (1 build conservee a la fois) ---
if not exist ".\build" mkdir ".\build"
for /f "skip=1 delims=" %%i in ('dir /b /ad /o-d .\build') do (
    echo Suppression du dossier : %%i
    rmdir /s /q ".\build\%%i"
)
if not exist "%BUILDDIR%" mkdir "%BUILDDIR%"

:: --- Copie des assets ---
if not exist "%BUILDDIR%\TrucksBook" mkdir "%BUILDDIR%\TrucksBook"
xcopy /E /I /Y ".\TrucksBook\*" "%BUILDDIR%\TrucksBook\"
xcopy /I /Y ".\LauncherSrc\tomove\Updater.bat" "%BUILDDIR%\"
powershell -Command "Compress-Archive -Path 'TGE-Mod' -DestinationPath '%BUILDDIR%\tge_mod_%FILENAMEE%.zip'"
echo {"Email": "","Password": ""} > "%BUILDDIR%\credentials.json"

:: --- Publish ---
dotnet publish .\LauncherSrc\LauncherSrc.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%BUILDDIR%"
if errorlevel 1 (
    echo *** Publish echoue. ***
    exit /b 1
)
echo Publish OK : %BUILDDIR%

:: --- Tests (apres le build, sur l'artefact construit) ---
echo.
echo === Tests ===
if exist ".\tests-credentials.json" (
    echo tests-credentials.json present : tests d'integration FlaUI inclus.
    copy /Y ".\tests-credentials.json" "%BUILDDIR%\credentials.json" >nul
    dotnet test ".\LauncherTests\LauncherTests.csproj" --nologo -v minimal
) else (
    echo tests-credentials.json absent : tests d'integration ignores.
    dotnet test ".\LauncherTests\LauncherTests.csproj" --nologo -v minimal --filter "Category!=Integration"
)
set TEST_RC=%errorlevel%

:: Restaurer credentials.json vide pour que le zip publie ne contienne pas les creds de test
echo {"Email": "","Password": ""} > "%BUILDDIR%\credentials.json"

if not "%TEST_RC%"=="0" (
    echo.
    echo *** Tests en echec. Build interrompu, pas de zip. ***
    exit /b 1
)

:: --- Zip final ---
powershell -Command "Compress-Archive -Path '%BUILDDIR%\*' -DestinationPath '%BUILDDIR%\LauncherTGE_%FILENAMEE%.zip' -CompressionLevel Optimal"

echo.
echo === Build OK ===
start "" explorer.exe /select,"%BUILDDIR%\LauncherTGE_%FILENAMEE%.zip"
endlocal
exit /b 0
