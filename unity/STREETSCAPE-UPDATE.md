# Bahnhofsviertel: Straßen, Boden und Vegetation

## Gestalterische Richtung

Ein bewohntes, einfaches Bahnhofsviertel mit reparierten Straßen und gepflegten
öffentlichen Wegen. Die Materialien unterscheiden Fahrbahn, Gehweg, Platz,
Pflanzbeet und bewachsenen Rand. Die Fassaden und der Straßenplan bleiben
Ausgangspunkt des Aufbaus.

Als Referenz dienen die Natursteinflächen, punktuellen Baumpflanzungen und
Sitzbereiche der [Vorplätze Ostkreuz](https://gruen-berlin.de/projekte/urbane-freiraeume/vorplaetze-ostkreuz/planung-entwicklung)
sowie die ruhige, graue Materialpalette am [Bahnhofsvorplatz Neu-Isenburg](https://www.ipach-mayerhofer.de/bahnhofsvorplatz).
Die Referenzbilder werden nicht als Spieltexturen verwendet.

## Neue Assets und Ausarbeitung

- Fotobasierter Asphalt, Natursteinpflaster, bewachsener Boden und Lauberde mit
  eigenen Normal- und Rauheitsdaten. Die Originalkarten sind von Poly Haven
  unter [CC0](https://polyhaven.com/license); die Herkunft ist unter
  `ZeroRise/Assets/ZeroRise/Art/Streetscape2K/SOURCE.md` dokumentiert.
- Eigene modellierte Bordsteinprofile mit abgeschrägter Sichtkante und Fugen,
  straßenseitige Steinrinnen, vertiefte Gitterabläufe, bündige Schachtdeckel
  und einzelne begrenzte Asphaltreparaturen.
- Bündige Steinbänder teilen den Bahnhofsvorplatz in kleinere Bereiche.
  Die vorhandenen Gehwege und Übergänge behalten ihre begehbaren Höhen.
- Drei eigene Baummodelle mit unterschiedlich geformten Kronen, modellierten
  Stammverzweigungen und einzeln ausgerichteten Blattflächen. Jede Form besitzt
  zwei Detailstufen; die Blatttexturen stammen aus dem vorhandenen Naturatlas.
- Gusseiserne Baumroste an den beiden runden Baumgärten, mit freier Stammöffnung.
- Zwei neue eigene Unterwuchsmodelle: gebogene Grasbüschel und blühende
  Stauden mit modellierten Stängeln und Blütenständen. Zusammenhängende
  Unterpflanzungen in Beeten und geschützten Rändern. Pflanzen
  werden nur auf den tatsächlich getroffenen Erdflächen verteilt. Wege,
  Hauszugänge, Fahrbahn und der innere Bereich der Baumroste bleiben frei.
- Die großen Anschlussgrundstücke besitzen ein unterteiltes Bodennetz mit
  Weltkoordinaten für die Materialmischung. Damit hängen Rasen-/Erdübergänge
  nicht mehr an den vier Ecken einer großen Bodenplatte.

## Aufbau

`BahnhofStreetscape.Apply` läuft nach `WestbruchAuditCompletion.Apply` und vor
Navigation und Renderbündelung im vorhandenen `BahnhofDistrictRework`.
Materialien, Profile, Baummodelle und zusammengefasste Pflanzen werden als
Projektassets gespeichert. Die Platzierung findet im Editor statt, nicht
bei jedem Spielstart. Der zuvor eingebaute Admin-Flugmodus bleibt erhalten.

Die zusätzliche Bepflanzung ist in 20-Meter-Bereiche mit zwei Detailstufen
aufgeteilt. Entfernte Bereiche verwenden ein Drittel der Pflanzen und werfen
keine zusätzlichen Kleinstschatten; nahe Pflanzen behalten ihre volle Geometrie.
Diese Bereiche bleiben bei der großen statischen Renderbündelung getrennt,
damit die Kamera außerhalb des Bildes liegende Pflanzen ausblenden kann.
Die neuen Straßen-, Stein- und Erdmaterialien sind außerdem in die vorhandene
Regensteuerung aufgenommen; Pflanzerde verwendet die zurückhaltendere
Bodenreaktion statt der stärkeren Glättung von Pflaster.

## Prüfung

Die Sichtprüfung erfolgt mit der gebauten nativen Mac-App, einschließlich
Straßenperspektiven, Bodennahaufnahmen, Tag-, Nacht- und Regenansicht. Die
bauliche Wurzelprüfung berücksichtigt auch die tatsächliche Erde innerhalb
der erhöhten Pflanzbeete.

- Vollständiger Kartenrundlauf vor den letzten optischen Korrekturen:
  `ZeroRise/QA/streetscape-final/bahnhof-validation.txt`: 121 Prüfungen bestanden,
  0 protokollierte Laufzeitfehler, 93 native Aufnahmen. 52 tatsächliche
  CharacterController-Abschnitte im Bestandsviertel sowie Bahnhofszugang,
  Haupttreppe, Gartenanschlüsse und acht echte Orbitkameraansichten am Damm.
- Abschließender Mac-Build: `43fee6edff33457bbcaae7046ac91343`, erstellt am
  2. November 2026 um 17:36 UTC. 0 Build-Fehler, 6 nicht blockierende
  Build-Warnungen. Protokoll: `ZeroRise/Logs/streetscape-verified-build.log`.
- Die 22 baulichen Prüfungen dieses Builds sind bestanden. Dabei wurden
  9.237 tatsächlich referenzierte Pflanzenwurzeln gegen die vorhandenen
  Bodenflächen geprüft; zusätzlich erfolgten die vorhandenen Fundament-,
  Dach-, Stützen-, Schrift- und Überlappungsprüfungen.
- Maßgeblicher Abschlussordner für die letzten Material-, Ablauf-, Gras-
  und Detailstufenkorrekturen: `ZeroRise/QA/streetscape-verified-release/`.
- Dort sind alle 54 abschließenden Spielprüfungen bestanden; es wurden keine
  Laufzeitfehler protokolliert. 39 native Spielaufnahmen umfassen zusätzlich
  vier Bodennah-/Detailperspektiven. `streetscape-update.jpg` kombiniert vier
  unveränderte, verkleinerte Aufnahmen zur Übersicht.
- Die abschließende viersekündige lokale Messung im beregneten Vorplatzblick
  bei 1600 × 900 Pixeln ergab durchschnittlich 28,4 ms pro Bild, 95. Perzentil
  33,5 ms. Die vorherige Messung ohne Aufteilung/Detailstufen hatte 39,8 ms
  beziehungsweise 50,4 ms ergeben; die Wetterzustände dieser beiden Läufe
  sind wegen der verbesserten Regenprüfung unterschiedlich. Das ist keine
  allgemeine Leistungszusage oder ein geräteübergreifender Benchmark.

Eine Zwischenprüfung hatte noch zu kurze Regenzeiten verwendet. Für die
abschließende Materialprüfung wird das Testwetter zuerst vollständig getrocknet
und danach über ausreichend Spielzeit beregnet. Erst dann werden tatsächliche
Materialfarbe und Rauheitsreaktion verglichen. Dies ändert keine gespeicherten
Wettereinstellungen des Spielers und beschleunigt den Regen im normalen Spiel
nicht.

Die Änderungen liegen lokal in der Szene, den Projektassets und der gebauten
Mac-App. Es wurde nichts zu GitHub gepusht. Der vorhandene Starter
`Westbruch-spielen.command` startet diese Version; der Admin-Flug ist weiter
mit F und über das Esc-Menü erreichbar.
