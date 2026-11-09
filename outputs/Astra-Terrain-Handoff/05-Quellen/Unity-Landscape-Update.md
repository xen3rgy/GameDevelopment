# Waldsee und Bergland

Die Landschaft setzt östlich des Bahnhofsviertels an. Vom Gewerbehof und den
hinteren Gärten führen Wiesen in bewaldete Hügel. Hinter dem Waldsee liegen
felsige Höhenzüge mit einer unregelmäßigen Schneegrenze. Die vorhandenen
Straßen, Grundstücke und Bahnanlagen behalten ihre bisherigen Höhen.

## Erkunden

- Das Spiel wie bisher mit `Westbruch-spielen.command` starten.
- Mit **F** den vorhandenen Admin-Flug einschalten; **WASD** bewegen,
  **Leertaste** steigen, **Strg** sinken, **Shift** schneller fliegen.
- Östlich vom Bahnhofsviertel Richtung Berge fliegen. Der große Waldsee liegt
  in der ersten Senke; ein schmaler Fußweg führt von den äußeren Wiesen ans Ufer.
- Zeit und Wetter können weiterhin im vorhandenen Menü umgestellt werden.
  Wind und Gewitter bewegen die Seeoberfläche stärker. In hohen Berglagen
  wird Regen allmählich durch Schneeflocken ersetzt.

## Umsetzung

- Gemeinsames deterministisches Höhenprofil für Gelände, Seebecken und Tests.
- Bodenflächen mit realen Mesh-Kollisionen; verbindende Kanten teilen ihre
  Höhen, bestehende Stadtfundamente bleiben außerhalb des neuen Reliefs.
- Vier Materialienbenen nach Hangneigung und Höhenlage: Waldboden, Gras,
  fotografierter Fels und Schnee. Weltkoordinaten, verzerrte Texturkoordinaten
  und großflächige Farbschwankungen verringern sichtbare Kachelwiederholungen.
- Drei im Projekt erzeugte Fichtenvarianten, vorhandene Laubbaumvarianten,
  mehrere Sichtweitenstufen, unregelmäßige Bestände und Lichtungen.
- Ufersteine, Gräser und Schilf sitzen auf dem erzeugten Geländekollider.
- Native HDRP-Wasserfläche; das Gelände verdeckt ihren rechteckigen Rand.
- Bestehender Wetterzustand steuert die Seeoberfläche und die lokale
  Niederschlagsart. Kein zusätzliches konkurrierendes Wettersystem.

Quellen und Lizenzen der fotografierten Oberflächen stehen in
`ZeroRise/Assets/ZeroRise/Art/Streetscape2K/SOURCE.md`.
Die Landschaftsshader basieren auf Unity HDRP 17.6, siehe
`ZeroRise/Assets/ZeroRise/Art/HDRP-LICENSE.md`.

## Prüfungen

Der gezielte Standalone-Prüfmodus heißt `-landscape-review` und wird zusammen
mit `-bahnhof-qa <Ausgabeordner>` gestartet. Er kontrolliert Baumwurzeln,
die vollständig geschlossene Wasserbegrenzung, den Geländeanschluss bei
x=600, Kamerasichtweite, Schneehöhe und Wetterreaktion. Aufnahmen erfolgen
im tatsächlich gebauten Mac-Spiel, einschließlich See bei Gewitter und
Schneefall im Hochland. Die Ergebnisse werden im jeweiligen QA-Ordner abgelegt.

Die neue Region ist Erkundungslandschaft. Neue Missionen, Schwimmen,
Zugverbindungen und eine jahreszeitlich veränderliche Schneedecke sind nicht
Teil dieses Updates.

## Abschließender Mac-Test vom 6. November 2026

Die Mac-Version wurde neu gebaut (Build-Zeit 14:53 UTC). Alle gezielten
Prüfungen bestanden; keine Laufzeitfehler in den vier abschließenden Runden.

- Landschaft: 5.618 Baumwurzeln geprüft, keine schwebenden Bäume;
  Geländeanschluss und Seeufer geschlossen; sechs Bodenproben am Seezugang bestanden.
- Wetter: See reagiert auf Sturm; in der Höhenlage ersetzt Schneefall den Regen.
- Bestehende Wiese: 434 von 434 Bodenproben bestanden, drei Laufstrecken mit
  dem Charakter und fünf Geländeanschlüsse geprüft.
- Dämmerung: Abend- und Morgenübergänge einschließlich laufender Uhr bei
  20-facher Geschwindigkeit erneut geprüft; keine Laufzeitfehler.
- Gemessener Waldblick über fünf Sekunden: bei 1600 × 900 durchschnittlich
  53,9 FPS (95. Perzentil der Bildzeit: 25,1 ms); bei 2940 × 1840
  durchschnittlich 24,2 FPS (50,0 ms). Diese Messung beschreibt genau diesen
  Blick auf diesem Mac, nicht eine garantierte Bildrate für die gesamte Karte.
  Volle Retina-Auflösung bleibt deutlich anspruchsvoller.

Originalaufnahmen und Protokolle liegen in `ZeroRise/QA/regional-release`,
`regional-retina`, `regional-meadow-regression` und `regional-twilight-regression`.

