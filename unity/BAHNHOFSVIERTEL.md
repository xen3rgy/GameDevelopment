# Bahnhofsviertel – Kartenaufbau und Prüfung

Die Erweiterung beginnt hinter der östlichen Bahnunterführung. Eine Werkstattstraße führt zur Bahnhofstraße; Güterhof, Bahnhofsvorplatz, Hintergärten und kommunale Betriebshöfe bilden einen zusammenhängenden Stadtteil. Der ursprüngliche Westbruch-Spielbereich bleibt erhalten. Zwei ehemalige Hintergrundhäuser innerhalb des neuen Viertels wurden aus ihren Quellen **und** den globalen Renderbatches entfernt, weil sie neue Gebäude und Höfe durchschnitten.

## Bauliche Korrekturen

- Zwölf Wohn-/Geschäftshäuser mit einzelnen Farbtönen, sauberen Kalkfassaden und Backstein, Walm- und Satteldächern, Gauben, geschlossenen Attiken und richtig eingebundenen Kaminen. Fenster, Rückeingänge, Balkone und Entwässerung sind auch an den Rückseiten ausgearbeitet.
- Durchgehende Fundamente reichen unter den Erdboden. Die zuvor sichtbaren rund 20 cm hohen Spalten an Hausunterkanten sind geschlossen. Mauerwerk verwendet durchgängige, realistisch skalierte Ziegelkurse.
- Werkstatt-, Garagen- und Gütergebäude besitzen eigene Fassaden, Dächer, Tore und unterstützte Ladeflächen. Grundstücksmauern, offene Einfahrten, Lagerhöfe, Fahrradschuppen und Hintergärten geben den Zwischenräumen eine Nutzung.
- Bahnhof mit getragenem Uhrengiebel, geschlossenen Hallenanschlüssen, gekacheltem Sockel, Fahrkartenschalter, Entwerter, Linienplan, Bänken und klar montierten Tafeln. Eine schmale Überdachung verbindet den Hallenausgang mit dem befestigten Bahnsteigzugang.
- Bahnsteige mit ausgearbeiteten Kanten, Fugen, Entwässerung, getragenen Dächern und Geländern. Rampen- und Podeststützen werden anhand des tatsächlichen Bodens an mehreren Punkten gegründet. Das obere Haupttreppenpodest überdeckt keine Stufen mehr.
- Bahndammfuß und ausgeschnittene Erdkanten sind geschlossen. Pflanzen aus dem ausgegrabenen Zugang sind entfernt; verbleibender Bewuchs wird an realen Bodenflächen neu verwurzelt. Vegetation wird erst nach dem Aufbau der Höfe auf Pflasterüberschneidungen geprüft.
- Zusammenhängendes Gelände setzt die Karte östlich, nördlich und westlich der Bahn fort. Gestaffelte Baumgruppen, Unterwuchs, Werkstätten und Betriebshöfe schließen die vorher offenen Blickrichtungen. Der Übergang besitzt keinen Grenzübergang.
- Zwei bepflanzte Baumgärten mit gebogenen Holzsitzen und Pflasterbändern gliedern den großen Bahnhofsvorplatz.

Ladenfronten, Fahrkartenschalter und Zug bleiben Umgebungsausstattung; ein neues Handels- oder Bahnbetriebssystem ist nicht Teil dieser Kartenkorrektur.

## Reproduzierbarer Aufbau

`ZeroRise.Editor.BahnhofDistrictRework.Apply` öffnet die gespeicherte Szene und baut die Erweiterung neu auf. `ApplyAndBuild` erzeugt danach die Mac-App. Auch `FinishDetailsAndBuild` führt jetzt den vollständigen Aufbau aus, damit alte Hallenmaße keine neueren Bauteile überschreiben.

Straße, Gehwege und Erde bestehen aus disjunkten Flächen. Neue Hofzufahrten treffen bündig auf die bestehenden Straßen; Hallenboden und hinterer Zugang teilen sich keine koplanare Fläche. Das alte Fernboden- und Bahndamm-Mesh wird nur im bearbeiteten Bereich angepasst. Retainierte Quellen und die tatsächlich sichtbaren Renderbatches werden gemeinsam korrigiert.

## Prüfung im gebauten Spiel

Die früheren Bildausschnitte erfassten weder alle Rückseiten noch die offenen Kartenränder. Ihre bestandenen Prüfungen waren deshalb kein ausreichender Nachweis für die Gesamtqualität.

Die aktuelle Kontrolle kombiniert:

- `BahnhofVisualQualityQA`: Schriftgrenzen, Montagen, echte Verbindungspunkte, Fahrzeugduplikate, Meshdaten, Dachflächen und überlappende Basisflächen.
- `BahnhofPlacementQA`: gemessene Fundamente, orientierte Gebäudegrundrisse, sichtbare alte Hintergrunddreiecke, 96 Projektionen auf echte Dachflächen, tatsächlich verwendete Graspflanzen-Wurzeln, Stützengründungen und Geländeanschlüsse auf beiden Bahnseiten.
- Native Mac-App mit `-bahnhof-qa <Ordner> -bahnhof-survey`: echte CharacterController-Laufwege, Haupttreppe aufwärts/abwärts, Navigation zu beiden Bahnsteigen, Bewohnerbewegung und Nachtlampen. Der Test verändert keine gespeicherten Wettereinstellungen.
- 31 native Aufnahmen: Straßen und Übergang in beiden Richtungen, Wohnhausrückseiten, Werkstätten, Höfe, Bahnhofshalle, Bahnhofsrückseite, Treppen, Rampen, Dachaufsichten, Bahnsteige, Kartenumfeld sowie Nacht und Regen. Sichtbare Probleme aus dem ersten Durchlauf wurden vor dem zweiten Build nachgearbeitet.

Erste neue Bildserie: `ZeroRise/QA/bahnhof-complete-review/`. Maßgeblich für den Abschluss ist `ZeroRise/QA/bahnhof-complete-release/`; Buildprotokoll: `ZeroRise/Logs/bahnhof-complete-release-build.log`. Die aktuelle Build-ID steht in `ZeroRise/Builds/macOS/Westbruch-build.json`.

## Start

Auf dem Mac `Westbruch-spielen.command` doppelklicken. Durch die östliche Unterführung gehen und nach dem Werkstatthof links/nördlich der Bahnhofstraße folgen. Die Änderungen liegen lokal im Unity-Projekt und in der gebauten Mac-App.

## Abschlussstand

Mac-Build `7fbe110e2e37420fb32171ae76f74b7c`: 0 Build-Fehler, 11 nicht blockierende Build-Warnungen (veraltete Unity-API-Aufrufe und doppelte HDRP-Shader-Keywords). 22 bauliche Prüfungen und 37 Prüfungen im nativen Spiel bestanden, 0 Laufzeitfehler. Die letzten 31 Aufnahmen wurden nach der Korrektur des schmalen Bodenanschlusses neben der Haupttreppe erneut erstellt.

Eine viersekündige Messung im nativen 1920 × 1080-Spiel ergab im getesteten Regenblick durchschnittlich 23,2 ms pro Bild (95. Perzentil 33,6 ms). Dies ist eine einzelne lokale Stichprobe, kein Benchmark für andere Geräte oder die gesamte Karte. Die grafische Ausarbeitung verwendet die vorhandenen Projektassets; entfernter Hintergrund und Baumformen bleiben vereinfacht.
