# Bahnhofsviertel – Kartenerweiterung

Die gespeicherte Westbruch-Szene ist um einen zusammenhängenden, begehbaren Stadtteil erweitert. Die ursprüngliche Karte und der Startpunkt bleiben erhalten. Hinter der vorhandenen Unterführung führt die Straße durch den Gewerbehof nach Osten und dann nach Norden zum Bahnhof. Ein Fußweg am Bahndamm verbindet Werkstätten, Innenhof und Vorplatz.

## Enthalten

- 14 zusätzliche Wohn-/Gewerbegebäude mit vorhandenen Westbruch-Fassaden und ergänzten geneigten Dächern, dazu ehemaliger Güterschuppen und Bahnhof.
- Ladenfronten für Bäckerei, Imbiss, Waschsalon, Lebensmittel, Fahrradreparatur, Pension, Café und Kiosk.
- Zugänglicher Güterhof mit Sitzgelegenheit und Alltagsgegenständen; Bushäuschen, Fahrräder und Parkplätze am Bahnhof.
- Tatsächlich begehbare Bahnhofshalle mit Sitzbänken, Fahrkartenautomaten als Ausstattung und einer Uhr, deren Zeiger die Spielzeit anzeigen.
- Zwei erhöhte Bahnsteige, durchgehendes ursprüngliches Gleis und zusätzliches Ausweichgleis, Treppen, Zugangsrampe und Fußgängerbrücke. Ein zweiteiliger Regionalzug steht an Gleis 1.
- 10 zusätzliche Bewohner, insgesamt 28, auf gemeinsam gebackener Fußgängernavigation; das vorhandene Verkehrsnetz führt durch die neuen Straßen.
- Straßen- und Bahnsteigleuchten reagieren auf Tageszeit und Wetter. Regenabschirmung und Pfützen berücksichtigen die erweiterte Karte.

Ladenfronten und Zug sind derzeit Umgebungsausstattung. Die Bahnhofshalle, Höfe, Gehwege und Bahnsteige sind erkundbar. Die Rampe ist eine spielbare Verbindung; mit rund 7,4 % Neigung ist sie keine nachgewiesen normgerechte barrierefreie Bauplanung.

## Technische Umsetzung

`BahnhofsviertelBuilder.Build` ergänzt die vorhandene Szene lokal. Die ursprüngliche Karte wird nicht neu generiert. `Polish` und `Refine` reparieren gezielt Navigation, die Anschlussnaht und bauliche Details. Geometrie wird in räumlichen Materialgruppen zusammengefasst; Charaktere, Fahrzeuge, Baum-LODs, Schrift und Uhrzeiger bleiben separat.

Am Anschluss wurden nur der benötigte östliche Zaunabschnitt und die querende Gehwegplatte geöffnet. Das Gelände unter der neuen Fahrbahn ist nivelliert. Der Fußgänger-NavMesh enthält weiterhin die ursprünglichen Straßen-Ausschlussflächen und Querungsstellen sowie neue Querungen im Bahnhofsviertel.

Der Wetter-Höhenatlas deckt nun beide Viertel ab (512 × 512 Zellen über 480 × 540 Meter). Bestehende Pfützenstandorte werden zuerst berücksichtigt, neue Standorte folgen im Bahnhofsviertel; das Gesamtbudget beträgt 260 Wasserstellen. Das eigentliche Sonnenuntergangsmodell bleibt unverändert.

## Spielprüfung

Die Mac-App kann mit `-bahnhof-qa <Ausgabeordner>` reproduzierbar geprüft werden. Dieser Modus schreibt keine Wetter-Spielstände. Er testet den Laufweg mit dem echten CharacterController von der Unterführung bis in die Bahnhofshalle, durchgängige native Navigationspfade zu Hof, Halle und beiden Bahnsteigen sowie bewegte Bewohner und Nachtbeleuchtung. Zusätzlich entstehen neun Aufnahmen bei Tag, Nacht und Regen.

Die frühen Tests fanden einen Laternenmast in der Fahrbahn am östlichen Anschluss und dort überhöhtes Restgelände. Der Mast wurde auf den Gehweg versetzt und das Gelände unter der Fahrbahn nivelliert. Die Ergebnisse des abschließenden Durchlaufs liegen unter `ZeroRise/QA/bahnhof-release-validation/`.

### Ergebnis des finalen Mac-Builds

- `bahnhof-release-validation`: 27 Prüfungen bestanden, keine fehlgeschlagen, 0 Laufzeitfehler. Alle sieben Teilstrecken des CharacterController-Laufwegs erreicht; native Navigationspfade zu Platz, Hof, Halle und beiden Bahnsteigen vollständig.
- `bahnhof-twilight-regression`: 31 Prüfungen bestanden, keine fehlgeschlagen, 0 Laufzeitfehler. Beide laufenden Übergänge bei 20× Tempo bestanden.
- Neun Spielaufnahmen erstellt; Straßen-/Bahnhofsansichten und Nachtbeleuchtung visuell geprüft.
- Finaler Mac-Build: `Westbruch and Bahnhofsviertel`, Build-GUID `d2a5d0d7b38e4726ba60401a7ae8e172`, 0 Build-Fehler, 5 Build-Warnungen. Die laufende Version wird über `ZeroRise/Builds/macOS/Westbruch-build.json` identifiziert.

Start auf dem Mac über `Westbruch-spielen.command`. Am ursprünglichen Startpunkt bleiben, die Unterführung nach Osten durchqueren, durch den Gewerbehof weitergehen und der Straße nach links/nördlich zum Bahnhofsplatz folgen.
