@echo off
set "ZERO_RISE_GAME=%~dp0ZeroRise\Builds\Windows\ZeroRise.exe"
if not exist "%ZERO_RISE_GAME%" (
  echo Im Unity Editor ZERO RISE ^> Windows-Prototyp bauen auswaehlen.
  pause
  exit /b 1
)
start "" "%ZERO_RISE_GAME%"
