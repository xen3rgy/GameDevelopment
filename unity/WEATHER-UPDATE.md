# Wetter und Licht · macOS-Update

Die Wetterdarstellung verwendet jetzt einen durchgehend veränderten Wolkenkörper statt sprunghaft wechselnder Wolken-Presets. Bewölkung baut sich vor dem Regen auf; Wind und Regen reagieren zeitversetzt. Der Nebel blendet eigenständig ein und aus.

Direkte Sonne nimmt bei dichter Bewölkung stärker ab. Ein breites, kühleres Fülllicht macht Schatten lesbar, während die reduzierte Aerosoldichte den bisherigen braunen Wetterfarbstich verringert. Nachtlicht und indirekte Aufhellung sind angehoben. Die Lichtsteuerung aktualisiert sich häufiger, um beschleunigte Tageszeiten ruhiger darzustellen.

Der Regen verwendet mehr, kürzere und feinere Tropfen mit höherer Sichtbarkeit. Exponierter Asphalt erhält mehr Glanz bei geringerer pauschaler Verdunklung. Bestehende Dachabschirmung, gespeicherte Restnässe und unterschiedliche Trocknung von Erde und Straßen bleiben erhalten.

Die Wettersteuerung heißt jetzt „Wetter & Tageszeit“. Bestehende Spielstände verwenden dasselbe Speicherformat. Das Spieltempo für Figur, Fahrzeuge und Physik bleibt unverändert.

Die Prüfung der Release-Metadaten berücksichtigt den macOS-App-Pfad. Für reproduzierbare Bildvergleiche kann der gebaute Player mit `-world-weather-visual-review <Ausgabeordner>` gestartet werden. Dieser Modus verwendet die vorhandenen Wetterprüfungen, speichert keine Weltänderungen und läuft auch ohne Fensterfokus weiter.

## Ergänzter Abschluss

Pfützen verwenden jetzt transparentes HDRP/Lit mit tatsächlicher Umgebungsbeleuchtung, Normalen für feine Wasserbewegung und aktivierten Reflexionspasses. Sie werden anhand von Bodenhöhe, flachen Auflageflächen, Dachschutz und Bordsteinnähe verteilt. Unterschiedliche Rückhaltefaktoren steuern ihre Ausdehnung bei wechselndem Wasserstand. Dies ist ein begrenztes Oberflächenmodell, keine vollständige hydraulische Simulation.

Regen erzeugt kurzlebige Bodenspritzer an seinen ermittelten Auftreffpunkten. Bewegte Fahrzeuge erzeugen auf nassem Untergrund begrenztes Reifensprühwasser. Beide Effekte teilen sich maximal 256 Partikel. Unitys integriertes Partikelsystem-Modul ist dafür aktiviert.

Pflanzenbewegung, Regenrichtung und Windgeräusch verwenden dieselbe Wetterwindstärke beziehungsweise Böenkurve. Fahrzeug- und Straßenbeleuchtung reagiert zusätzlich auf dunkles Regenwetter. Die Testbilder enthalten eine Nahansicht einer Pfütze.

## Prüfung der gebauten Mac-App

- `weather-update-review`: 31 Prüfungen bestanden, 0 fehlgeschlagen; Runtime errors: 0.
- `weather-update-twilight`: 30 Prüfungen bestanden, 0 fehlgeschlagen; Runtime errors: 0.

Der Wetterdurchlauf umfasst Sonne, Regen, Unterstand, Gewitter, Morgennebel, Abend, Nacht und Restnässe. Der Dämmerungsdurchlauf prüft 13 feste Tageszeiten und zusätzlich durchgehende Übergänge bei 20× Tempo. Die Bilder wurden visuell geprüft.

## Abschlussprüfung

- `weather-final-validation`: 66 Prüfungen bestanden; Runtime errors: 0.
- `weather-complete-final`: 33 Prüfungen bestanden; Runtime errors: 0.

Die finale Pfützen-Nahaufnahme zeigt die Spiegelung der Brückenstruktur auf der Wasseroberfläche. Der Mac-Build wurde erfolgreich erstellt (0 Build-Fehler, 2 Warnungen). Die Szene entspricht weiterhin inhaltlich dem ursprünglichen Git-LFS-Objekt; verändert wurden Wettercode, Wettermaterialien und das aktivierte Partikelsystem-Modul.

## Nachtbeleuchtung: Reichweite und Lichtverteilung

Die bisherige Auswahl der vier nächsten Verkehrsautos und acht nächsten Laternen ist entfernt. Bodenlicht von Autos bleibt bis 140 Metern Betrachtungsabstand vollständig erhalten und läuft zwischen 140 und 240 Metern weich aus. Straßenlaternen blenden erst zwischen 170 und 270 Metern aus. Diese Betrachtungsabstände sind unabhängig von der physischen Ausdehnung des Lichtkegels (Scheinwerfer 62 Meter, Laternen 23 Meter).

Autos verwenden ein gemeinsames prozedurales Abblendlichtprofil mit weicher, asymmetrischer Hell-Dunkel-Grenze. Die sichtbaren Linsen sind heller, der Lichtbeitrag auf der Straße ist dazu abgestimmt. Nahe Scheinwerfer werfen begrenzte Schatten; entfernte Lichtbeiträge bleiben sichtbar. Dezente rote Rücklichtbeiträge reagieren bei Verkehrsautos zusätzlich auf Bremsen.

Bestehende Hauseingänge und eingerichtete Fenster erhalten kleine warme Lichtquellen. Sie verbessern die Orientierung und machen bewohnte Bereiche erkennbar. Die Nachtaufhellung ist moderat erhöht; nasser Asphalt verwendet räumlich variierende Rauheit statt eines einheitlich glatten Spiegels.

Der Test `-night-lighting-qa <Ausgabeordner>` nimmt dasselbe angehaltene Auto aus 15, 35, 64, 66, 95 und 150 Metern auf. Bei 64, 66 und 95 Metern wird zusätzlich der tatsächliche Helligkeitsbeitrag der Scheinwerfer auf einer Straßenstelle anhand eines Kontrollbildes ohne diesen Beitrag geprüft. Weitere Ansichten zeigen trockene und nasse Straßen, Fensterlicht und Wasserreflexionen.

Zusätzlich wird das Emissionsmaterial der Frontlinsen pro Fahrzeug auch in zusammengefassten Fahrzeugmeshes ersetzt. Damit stimmen sichtbare leuchtende Scheinwerfer und die auf den Boden projizierten Lichtkegel überein. Die Bildkontrolle integriert eine Bodenregion, damit zeitliche Bildglättung an einzelnen Kiesel- oder Markierungspixeln die Messung nicht dominiert.

### Prüfung der finalen Nachtfassung

- 40 Prüfungen bestanden, keine Laufzeitfehler.
- Der gerenderte Bodenlichtbeitrag wurde bei 64, 66 und 95 Metern durch Kontrollbilder bestätigt.
- 33 aktive Straßenlaternen, 13 eigene Emissionsmaterialgruppen für fahrbare/verkehrende Autos in der Testszene.
- Stabile Nachtübersicht: 16,67 ms mittlere Bildzeit bei 1280×720 über 181 Frames (etwa 60 FPS; kurze Messung einer einzelnen Ansicht).
- Vergleichsaufnahmen: `ZeroRise/QA/night-lighting-final-proof/`.

## Dunkler Himmel und unterschiedliche Lichtbereiche

Die Nachtfassung trennt Himmelsbeleuchtung und Spielbarkeit auf der Karte. Das diffuse Aufhelllicht und die indirekte Gegenaufhellung wirken ausschließlich auf Oberflächen. Der bisherige starke blaue Himmelsanteil entfällt. Das eigentliche Mondlicht bleibt bei vollständiger Nacht unter 0,3 Lux und wird bei dichter Bewölkung schwächer. Während der Dämmerung wird seine Intensität an die Kamerabelichtung angepasst.

Ein prozedural erstellter Sternen-Cubemap und eine dezente Mondoberfläche gehören jetzt zum Projekt. Die Sterne erscheinen bei klarer Nacht und verschwinden bei dichter Bewölkung beziehungsweise Nebel. Die Assets liegen unter `Assets/ZeroRise/Resources` und benötigen keine externen Downloads.

Auf der Karte unterscheiden sich warmes Laternenlicht, kühleres Licht im Geschäftsbereich und das Licht der Haltestelle. Vorhandene Eingänge und Schaufenster beleuchten angrenzende Gehwege stärker. Eine kleine Leuchte sitzt unter dem Dach der bestehenden Haltestelle. Diffuse, nicht spiegelnde Lichtbeiträge unter Laternen nähern das von Straßenflächen auf Fassaden zurückgeworfene Licht an. Der Grundkontrast orientiert sich an einer filmisch beleuchteten Spielstadt: dunkler Himmel, erkennbare Oberflächen und bewusst gesetzte Lichtinseln.

Die Prüfung `-night-sky-qa <Ausgabeordner>` erstellt getrennte Aufnahmen von Himmel, Straße, Schaufenster und Haltestelle. Sie prüft sichtbare Sterne, einen dunklen gerenderten Himmel, schwaches Mondlicht, sternlosen bedeckten Himmel und die Lesbarkeit der Karte.

### Prüfung der dunklen Nachtfassung

- `dark-night-final`: 23 Prüfungen bestanden, 0 fehlgeschlagen; Runtime errors: 0.
- `dark-night-twilight`: 31 Prüfungen bestanden, 0 fehlgeschlagen; Runtime errors: 0.

Der final gerenderte klare Nachthimmel hatte eine mittlere sRGB-Luminanz von rund 0,00124; der Bildtest erkannte 278 helle lokale Sternpunkte. Bei dichter Bewölkung war die Sternsichtbarkeit null. Die beiden laufenden Dämmerungsprüfungen bei 20× Tempo bestanden ebenfalls. Die Aufnahmen von Straße, Haltestelle, Laden und Himmel wurden visuell geprüft.

## Gleichmäßiger Sonnenuntergang von 20:00 bis 20:40

Die erneute blaue Aufhellung nach Sonnenuntergang entstand durch das Zusammenspiel der Belichtung mit einer zusätzlichen Lichtquelle, die auch die Atmosphäre beleuchtete. Diese Tagesaufhellung wirkt jetzt ausschließlich auf Oberflächen. Eine eigene atmosphärische Sonne erzeugt das Nachglühen anhand des Sonnenstands; ihre Helligkeit wird gemeinsam mit der Kamerabelichtung abgeglichen. Die übrigen Lichtquellen der Karte verändern die Himmelshelligkeit nicht.

Die Sonne erreicht um 20:00 den Horizont. Anschließend verblasst das warme Nachglühen, während der Himmel kontinuierlich dunkler wird. Die Sterne beginnen erst bei einer Sonnenhöhe unter −2 Grad zu erscheinen und werden bis −8 Grad weich eingeblendet. Bewölkung und Nebel reduzieren weiterhin ihre Sichtbarkeit. Die Aktualisierung der Atmosphäre erfolgt viermal pro Sekunde. Lichtquellen und diffuse Nachtaufhellung halten die Straßen während des Übergangs erkennbar.

### Prüfung der neu gebauten Mac-App

- `sunset-final-validation`: 23 Prüfungen bestanden; Runtime errors: 0. Feste Aufnahmen im Abstand von fünf Spielminuten von 19:45 bis 20:45 bei klarem Himmel, Bewölkung und Sturm. Zusätzlich laufende Übergänge von 20:00 bis 20:41 bei normalem und 20× Spieltempo geprüft: keine erneute flächige Himmelsaufhellung und kein abrupter Helligkeitsabfall.
- `sunset-final-twilight`: 31 Prüfungen bestanden; Runtime errors: 0. Kontrolliert feste Morgen-/Abendzustände sowie durchgehende Dämmerungsübergänge und die Erkennbarkeit des Bodens.
- Die gerenderten Himmelsaufnahmen um 20:00, 20:10, 20:20, 20:30 und 20:40 sowie Straßenansichten wurden visuell geprüft. Die geringe spätere Zunahme einzelner heller Pixel stammt vom Einblenden der Sterne.

Reproduzierbar mit `-sunset-transition-qa <Ausgabeordner>`. Der Test hält Verkehr und Wolkenbewegung an, lässt die Renderzeit aber normal weiterlaufen, damit HDRP die Atmosphäre aktualisieren kann. Frühe Versuche mit angehaltener Renderzeit in `sunset-regression-before` und `sunset-transition-final` sind jeweils als ungültig markiert; sie sind kein Nachweis für das Verhalten im Spiel. Die eigentlichen Vorheraufnahmen liegen unter `sunset-before`.

Der Mac-Build enthält die korrigierten Skripte. Die gespeicherte Westbruch-Szene wurde nicht verändert; ihr SHA-256 entspricht weiterhin `7d65fdfc8f4f2e071abf5400ff01d4093b2326dde0779ece37d8e21f0a6fdd81`.
