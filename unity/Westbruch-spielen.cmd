@echo off
setlocal
set "ZERO_RISE_DIR=%~dp0ZeroRise\Builds\Windows"
set "ZERO_RISE_GAME=%ZERO_RISE_DIR%\ZeroRise.exe"
if not exist "%ZERO_RISE_GAME%" (
  echo Kein Windows-Build gefunden. In Unity ZERO RISE ^> Windows-Prototyp bauen auswaehlen.
  pause
  exit /b 1
)
if not exist "%ZERO_RISE_DIR%\Westbruch-build.json" (
  echo Dieser Build ist veraltet. Den aktuellen Westbruch-Windows-Build erstellen.
  pause
  exit /b 1
)
start "ZERO // RISE - Westbruch" /D "%ZERO_RISE_DIR%" "%ZERO_RISE_GAME%" %*
