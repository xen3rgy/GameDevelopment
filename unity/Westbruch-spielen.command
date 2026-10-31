#!/bin/zsh

set -u

ROOT_DIR="$(cd -- "$(dirname -- "$0")" && pwd)"
PROJECT_DIR="$ROOT_DIR/ZeroRise"
BUILD_DIR="$PROJECT_DIR/Builds/macOS"
GAME_APP="$BUILD_DIR/ZeroRise.app"
BUILD_INFO="$BUILD_DIR/Westbruch-build.json"
BUILD_LOG="$PROJECT_DIR/Logs/macOS-build.log"

NEEDS_BUILD=0
if [[ ! -d "$GAME_APP" || ! -f "$BUILD_INFO" ]]; then
  NEEDS_BUILD=1
else
  NEWER_SOURCE="$(/usr/bin/find "$PROJECT_DIR/Assets" "$PROJECT_DIR/ProjectSettings" "$PROJECT_DIR/Packages" -type f -newer "$BUILD_INFO" -print -quit 2>/dev/null)"
  [[ -n "$NEWER_SOURCE" ]] && NEEDS_BUILD=1
fi

if [[ "$NEEDS_BUILD" -eq 1 ]]; then
  UNITY_VERSION="$(/usr/bin/sed -n 's/^m_EditorVersion: //p' "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt")"
  UNITY_EDITOR="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"

  if [[ ! -x "$UNITY_EDITOR" ]]; then
    UNITY_EDITOR="$(/usr/bin/find /Applications/Unity/Hub/Editor -maxdepth 8 -path '*/Unity.app/Contents/MacOS/Unity' -type f -print 2>/dev/null | /usr/bin/sort | /usr/bin/tail -n 1)"
  fi

  if [[ ! -x "$UNITY_EDITOR" ]]; then
    print "Unity wurde unter /Applications/Unity/Hub/Editor nicht gefunden."
    print "Bitte installiere Unity Hub und starte diese Datei danach erneut."
    read "?Zum Schließen Eingabetaste drücken … "
    exit 1
  fi

  print "Der macOS-Build fehlt. Unity erstellt ihn jetzt; das kann einige Minuten dauern."
  mkdir -p "$BUILD_DIR" "$PROJECT_DIR/Logs"
  if ! "$UNITY_EDITOR" -batchmode -quit -projectPath "$PROJECT_DIR" \
      -executeMethod ZeroRise.Editor.WestbruchMacBuild.Build \
      -logFile "$BUILD_LOG"; then
    print "Der Unity-Build ist fehlgeschlagen. Details stehen hier:"
    print "$BUILD_LOG"
    /usr/bin/tail -n 30 "$BUILD_LOG"
    read "?Zum Schließen Eingabetaste drücken … "
    exit 1
  fi
fi

if [[ ! -d "$GAME_APP" || ! -f "$BUILD_INFO" ]]; then
  print "Unity hat den macOS-Build nicht vollständig erstellt. Details: $BUILD_LOG"
  read "?Zum Schließen Eingabetaste drücken … "
  exit 1
fi

/usr/bin/open -a "$GAME_APP" --args "$@"
