# Bahnhofsviertel – überarbeitete Kartenerweiterung

Die Erweiterung schließt hinter der vorhandenen Unterführung an Westbruch an. Werkstattstraße, Bahndammweg und Hinterhof führen zum Bahnhofsvorplatz. Der bisherige Startbereich, die ursprünglichen Häuser und die Spielfunktionen bleiben erhalten.

## Architektur und Ausstattung

- Zwölf eigenständig aufgebaute Wohn-/Geschäftshäuser mit vier Fassaden- und Dachfamilien; keine kopierten Wohnhaus-Prefabs. Fassadenöffnungen, Laibungen, Rahmen und Sohlbänke sind geometrisch ausgearbeitet, die Texturzuordnung läuft über die gesamte Fassade.
- Werkstatt, Garagenzeile und Güterschuppen besitzen eigene Fassaden, Tore, Dachanschlüsse und Ausstattung.
- Bahnhof mit Mittelgiebel, Uhr, getragenem Vordach, tatsächlichen Fensteröffnungen, Halle mit Decke, Ticketschalter, Sitzbänken und Wandtafeln.
- Zwei Bahnsteige mit Betonunterbau, separater Deckplatte, Kanten, getragenen Dächern, Lichtgehäusen, Schildern und Entwässerung. Der Regionalzug besitzt geformte Wagenkästen, Führerstände, Drehgestelle und Türen.
- Hallenausgang und Treppe sind durch einen befestigten, in den Bahndamm eingeschnittenen Weg mit Stützwänden verbunden.
- Vorzonen und Pflanzbeete, passende Westbruch-Sträucher, Bodenbewuchs, Straßenabnutzung, Entwässerung, Reparaturecke im Hof und ein ausgearbeiteter Kiosk/Busunterstand.
- 28 Bewohner auf gemeinsamer Navigation; bestehendes Verkehrsnetz und Wetter-/Lichtsystem berücksichtigen beide Viertel.

Ladenfronten, Ticketschalter und Zug sind Umgebungsausstattung. Halle, Höfe, Gehwege und Bahnsteige sind begehbar.

## Behobene Ursachen

Die erste Fassung enthielt übereinanderliegende Bodenplatten, einen Pflasterstreifen quer über die Fahrbahn, doppelt sichtbare Fahrzeugteile, übernommene fremde Straßenschilder und alte Dachaufbauten unter neuen Dächern. Die Schrift wurde anhand einer Zeichenanzahl geschätzt und überschritt teilweise die Träger.

Straße, Pflaster und Erde werden jetzt als disjunkte Flächen mit fortlaufenden Weltkoordinaten für die Texturen erzeugt. Höhenwechsel sind seitlich geschlossen. Das darunterliegende Terrain liegt tiefer; alte Fernbodenflächen werden nur im überarbeiteten Fußabdruck entfernt. Der Hallenvorplatz und der Bahndammweg sind durchgehend unterstützt.

Alle 52 Texttafeln verwenden physische Träger und messen die tatsächlichen Schriftgrenzen. Die Schrift bleibt auch nach einem Font-Atlas-Neuaufbau innerhalb der Tafel. Türsturz-, Wand-, Pfosten- und Hängemontagen sind ausdrücklich ausgearbeitet.

Fahrzeuge behalten ihre eigenen aktiven Geometriebatches; ihre deaktivierten Einzelquellen werden nicht wieder eingeblendet. Neue Bauteile werden in räumlichen Materialgruppen zusammengefasst. Baum-/Strauch-LODs, Figuren, Fahrzeuge und bewegte Uhrzeiger bleiben separat.

## Reproduktion und Prüfung

`ZeroRise.Editor.BahnhofDistrictRework.Apply` baut die Erweiterung aus den vorhandenen Quellen erneut auf. `ApplyAndBuild` erzeugt anschließend die Mac-App. Beide erhalten die ursprüngliche Westbruch-Karte. Die alte Refine-Funktion verweist bei installierter neuer Fassung auf diesen Ablauf.

`BahnhofVisualQualityQA` prüft Schriftgrenzen, Tafeln, lose alte Texte, physische Montage am Hallenausgang, Bodenproben an den zuvor fehlerhaften Verbindungen, Fahrzeugduplikate, endliche Meshdaten, Dachausrichtung und koplanare Basisflächen. Bericht: `ZeroRise/QA/bahnhof-rework-authoring.txt`.

Die gebaute Mac-App lässt sich mit `-bahnhof-qa <Ausgabeordner>` prüfen. Sie speichert keine Wetteränderungen. Der Laufwegtest bewegt den echten CharacterController und prüft sowohl Zielposition als auch Höhe über dem unterstützenden Boden. Dazu kommen native Navigationspfade, Schriftgrenzen im Player, Bewohnerbewegung und Nachtbeleuchtung. Vierzehn Aufnahmen zeigen Übergang, beide Straßenseiten, Hof, Halle, Bahnhof, Bahnsteige und Brücke bei Tag sowie Vorplatz bei Nacht und Regen.

Vorheraufnahmen: `ZeroRise/QA/bahnhof-rework-before/`. Die Zwischenprüfung `bahnhof-rework-review` ist kein Abschlussnachweis; sie zeigte noch fehlenden Boden westlich des Platzes. Endprüfung: `ZeroRise/QA/bahnhof-rework-release/`. Der ergänzende Dämmerungsdurchlauf liegt unter `ZeroRise/QA/bahnhof-rework-twilight/`.

Der bisherige reine X/Z-Laufwegtest konnte ein Ziel trotz falscher Spielerhöhe als erreicht werten. Seine alten Ergebnisse belegen keine ausreichende visuelle Qualität. Die neue Prüfung berücksichtigt diesen Fehler ausdrücklich.

## Start

Auf dem Mac über `Westbruch-spielen.command` neu starten. Durch die Unterführung nach Osten gehen, den Gewerbehof durchqueren und der neuen Straße nach links/nördlich zum Bahnhof folgen.

Aktueller Mac-Build: `0ec4f1183fb74efab1edd67e18be2e0a`, 0 Build-Fehler, 5 Build-Warnungen. Maßgeblich ist `ZeroRise/Builds/macOS/Westbruch-build.json`.

### Abschlussprüfung der Überarbeitung

- 13 geometrische Qualitätsprüfungen bestanden, einschließlich Montage des Hallenschilds, tatsächlichem Boden vor dem Bahnhof und auf dem Bahndammweg, 52 gemessenen Schriftträgern und 240 Stichproben der Basisflächen.
- `bahnhof-rework-release`: 33 Spielprüfungen bestanden, 0 Laufzeitfehler. Alle sieben Laufwegabschnitte mit richtiger Bodenhöhe erreicht; Navigation zu beiden Bahnsteigen vollständig.
- `bahnhof-rework-twilight`: 31 Dämmerungsprüfungen bestanden, 0 Laufzeitfehler. Beleuchtung und Wettermodell blieben beim anschließenden Abschluss der Hallenanschlüsse unverändert.
- Finale native Aufnahmen von Vorplatz, Halleninnerem, Wohn-/Geschäftshäusern, Werkstatt, Hof, Bahnsteigen, Übergang und Nachtansicht wurden visuell geprüft. Die endgültige Bildserie liegt in `bahnhof-rework-release`.
- Der beim gesperrten Mac stehen gebliebene letzte Teststart wurde nach manuellem Entsperren neu gestartet; als Nachweis zählt der vollständig abgeschlossene Durchlauf.
