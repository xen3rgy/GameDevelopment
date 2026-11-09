# Waldsee – Anbindung an Westbruch

Der See ist vom Bahnhofsviertel aus ohne Flugmodus erreichbar. Es wurden keine Aufgaben oder Missionen ergänzt.

## Wege zum See

- **Zu Fuß:** Am nördlichen Ende der Hinteren Gasse den Wegweiser „WALDSEE / Uferweg“ nehmen. Der geschwungene Weg führt durch den Gehölzsaum zur Ankunftsbucht.
- **Mit dem Auto:** Durch den östlichen Bahnhof-Betriebshof fahren. Eine neue Öffnung in dessen östlicher Mauer führt auf die schmale Kieszufahrt. Sie endet an einer kleinen Park- und Wendefläche. Von dort geht der Weg rund 45 Meter weiter zum Ufer.
- **Am Wasser:** Der kurze Uferweg führt zur alten Verladekante. Sitzbänke, Holzanschläge für die Stellplätze und Wegweiser ergänzen den Ort.

## Gestaltung und technische Umsetzung

- Gemeinsame Wegachsen steuern Geländemodellierung, Oberflächen, Vegetationsabstände und Prüfungen.
- Gelände unter Zufahrt und Wegen wurde sanft angepasst; das feinere Geländeraster am Ufer verhindert steile Zwischenkanten.
- Eine zusammenhängende Oberflächengeometrie verbindet Wege, Parkplatz und Bucht ohne sich überlagernde Straßenstreifen.
- Kies wird an den Rändern mit dem vorhandenen Wald- und Grasboden überblendet; Texturmaßstab in Metern. Die äußere Schulter lässt den tatsächlichen Untergrund durchscheinen, damit keine eckige Auflage sichtbar bleibt. Am Betriebshof schließt der Kies bündig an die gerade Asphaltkante an.
- Gescanntes Kiesmaterial: Dario Barresi, [Gravelly Sand / Poly Haven](https://polyhaven.com/a/gravelly_sand), CC0. Herkunft und Importdaten stehen in `ZeroRise/Assets/ZeroRise/Art/Streetscape2K/SOURCE.md`.
- Bäume und Uferpflanzen halten Abstand zu Fahrbahn und Wegen. Zusätzlicher Bewuchs steht in unregelmäßigen, am Boden ausgerichteten Gruppen.
- Bestehende Wetterreaktion des Sees bleibt erhalten. Die Kiesfläche reagiert auf Regen über dasselbe Nässe-System wie der umliegende Boden.

## Nachvollziehbare Prüfung

- Editor-Prüfungen: `ZeroRise/QA/bahnhof-rework-authoring.txt`.
- Zehn Standalone-Prüfungen bestanden: durchgehende Charakterbewegung bis zum See und am Ufer; freie Fahrzeugdurchfahrt; durchgehender Boden; trockener Uferverlauf; echte Radphysikfahrt bis zum Parkplatz (maximal 11 cm seitliche Abweichung); keine Laufzeitfehler. Bericht: `ZeroRise/QA/lake-access-release/bahnhof-validation.txt`.
- Tatsächliche Spielaufnahmen nach der abschließenden Detailkorrektur: `ZeroRise/QA/lake-access-release/`.
- Aktueller Build-Nachweis: `ZeroRise/Builds/macOS/Westbruch-build.json`.

Start wie bisher über `Westbruch-spielen.command`.
