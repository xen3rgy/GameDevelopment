@echo off
set "ZERO_RISE_EDITOR=C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
if not exist "%ZERO_RISE_EDITOR%" (
  echo Bitte den Ordner ZeroRise in Unity Hub mit Unity 6000.6.5f1 oeffnen.
  pause
  exit /b 1
)
start "" "%ZERO_RISE_EDITOR%" -projectPath "%~dp0ZeroRise"
