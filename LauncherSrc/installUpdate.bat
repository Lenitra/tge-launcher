@REM Supprimer le .exe et tout les .zip du répertoire
del /Q /F "%~dp0*.exe"
del /Q /F "%~dp0*.zip"

@REM Dézipper le fichier dans /build
for %%f in ("%~dp0/build/*.zip") do (
    echo Décompression de %%f dans /build
    powershell -command "Expand-Archive -Path '%%f' -DestinationPath '%~dp0build' -Force"
)
@REM// TODO