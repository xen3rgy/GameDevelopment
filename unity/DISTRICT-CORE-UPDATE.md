# Bahnhofsviertel – Boden und Straßenraum

Dieses Update konzentriert sich auf die bespielte Stadt und den Bahndamm.

## Behobene Ursachen

- Die alte Vegetationsprüfung verglich Wurzeln mit dem Grundgelände, selbst
  wenn darüber Pflaster, Beton oder andere Bauteile lagen. Die abschließende
  Prüfung unterscheidet jetzt offene Erde von überbauten Flächen und prüft
  zusätzlich den Rand um den Pflanzenansatz.
- Die westliche Bahnabsperrung folgte dem ursprünglichen Einzelgleis und
  kreuzte das später ergänzte Ausweichgleis. Sie folgt jetzt dessen Außenseite
  und umgeht auch den Bahnsteigbereich.
- Beim Bahndamm waren alte Darstellungen in zusammengefassten Render-Meshes
  geblieben. Die alten Kopien werden vor dem Materialwechsel entfernt.
- Die Randsteinflächen waren nach innen ausgerichtet. Ihre Außenseiten werden
  jetzt korrekt gezeichnet. Die Randsteinführung wird aus den echten
  Fahrbahnrändern abgeleitet und berücksichtigt Nebenstraßen und Einmündungen.

## Gestaltung

- Zusammenhängende, räumlich projizierte Erde-, Gras- und Laubschichten am
  Bahndamm und im Viertel; Normalmaps und großflächige Farbvariation.
- Tatsächliches kleines Bodenrelief an den Böschungen, einschließlich der
  begehbaren Kollisionsfläche; Gleiskrone und Dammfuß bleiben angeschlossen.
- Durchgehender Asphalt und abgestimmtes Pflaster durch die Unterführung
  und den angrenzenden Gewerbehof.
- Kleine, unregelmäßige Grasgruppen auf den Böschungen, zurückhaltendere
  Pflanzensäume statt durchgehender Reihen gleicher breiter Blattbüschel.
- Die isolierte Betonrinne am Dammfuß wurde nach der Bildprüfung entfernt;
  der begrünte Übergang bleibt frei von unverbundenen Betonkanten.
- Bestehende Straßen, Häuser, Spielfunktionen und die äußere Landschaft bleiben
  Teil derselben Karte. Das ist kein Neubau des gesamten Spiels.

## Prüfung

Die Vorher-Aufnahmen stammen aus `ZeroRise/QA/core-before`.
Die Bauprüfungen stehen in `ZeroRise/QA/bahnhof-rework-authoring.txt`,
die gezielten Reparaturzahlen in `ZeroRise/QA/district-core-renewal.txt`.
Der fertige Mac-Build wurde mit zwei automatisierten Spielrunden geprüft:

- `ZeroRise/QA/core-release`: 13 gezielte Aufnahmen von Straßenanschlüssen,
  Böschungen, beiden Gleisverzweigungen, Pflanzbeeten und Bahnhofsvorplatz,
  einschließlich Regen und Nacht. Die Aufnahmen wurden visuell kontrolliert.
- `ZeroRise/QA/core-release-routes`: begehbare Verbindungen, Auf- und Abstieg
  an der Bahnhofstreppe, Bewohnerbewegung, Tages-/Nachtbeleuchtung und
  Regenreaktion des Pflasters. Alle Prüfungen bestanden, keine Laufzeitfehler.
- Bauprüfung: 19.750 Pflanzenansätze auf offenem Boden, 270 Gleisabschnitte
  ohne Zaunkontakt und 1.242 Randsteine ohne nach innen gerichtete Flächen.
- Bildzeit während der Laufprüfung: Mittel 30,6 ms, 95. Perzentil 41,8 ms
  bei 1600 × 900. Das entspricht im Mittel etwa 33 Bildern pro Sekunde;
  es ist keine Zusicherung für andere Auflösungen oder Geräte.

Build-Kennung: `0f019ebfa9244b879b5ea0e812b0516a`.
Start über `Westbruch-spielen.command`. Der Starter verweist auf den geprüften
Build; zum Abschluss waren keine Projektdateien neuer als dessen Build-Metadaten.
Die Änderungen sind lokal gespeichert und noch nicht zu GitHub übertragen.

Die Prüfungen beziehen sich auf die beschriebenen Bereiche und Fehlerklassen.
Der vorhandene stilisierte Gebäudebestand und Teile der Vegetation bleiben
erkennbar; dieses Update macht daraus keine fotorealistische Asset-Bibliothek.


## Korrektur anhand der sechs Nutzerbilder – 7. November 2026

- **Schwebende Pflanzen am Baum:** Die primitive Zylinderform verwendete einen
  CapsuleCollider. Bei breiten, flachen Erdbeeten bildete dieser eine unsichtbare
  Kuppel. Flache Zylinder bekommen jetzt die exakte Mesh-Kollision; beide runden
  Baumbeete sind neu erzeugt und auf Übereinstimmung zwischen sichtbarer Erde
  und Kollisionshöhe geprüft. Die Bepflanzung sitzt wieder auf der Erde.
- **Verzogene Bodentextur:** Die elf Meter starke Verzerrung der Texturkoordinaten
  entfällt. Die Texturen behalten ihren Maßstab auch an geneigten Flächen.
- **Rechteckige Farbwechsel:** Die lokale und regionale Bodendarstellung verwenden
  im Übergang dieselben Texturen, Maßstäbe, Farbtöne und pro Bildpunkt berechneten
  Mischgewichte. Unterschiedliche Vertexfarben einzelner Bodenplatten erzeugen
  dort keine Schnittkante mehr. Die äußeren Höhenbiome bleiben erhalten.
- **Rillen und Streifen am Bahnhof:** Die sich kreuzenden, separat aufgesetzten
  Zierbänder entfallen. Das Pflaster des Vorplatzes bleibt durchgehend.
- **Pflanzenreihen:** Die schmalen Reihen entlang des Dammwegs werden durch
  unregelmäßige Gruppen mit unterschiedlichen Breiten und Lücken ersetzt;
  auch die Beetpflanzen stehen nicht mehr auf einem festen Raster.

Neue gezielte Spielaufnahmen: `ZeroRise/QA/soil-seams-release` (18 Bilder).
Die Kameras erfassen ausdrücklich das runde Baumbeet auf Fußgängerhöhe,
den Vorplatz von oben, den Gartenhügel, den nördlichen Materialübergang und
die Verteilung entlang des Dammwegs. Diese Ansichten wurden visuell geprüft.
Der separate Test für flache Baumbeet-Kollisionen ist bestanden.

Aktuelle Build-Kennung: `6fcf92f0ce1b43a48f3568601445d12a`.

Abschließende Laufprüfung: `ZeroRise/QA/soil-seams-routes` bestanden,
54 Prüfungen ohne Laufzeitfehler. Wege, beide Treppenrichtungen, Bewohner,
Nachtbeleuchtung und Regenreaktion wurden im neuen Build getestet.
Bildzeit bei 1600 × 900: Mittel 27,1 ms, 95. Perzentil 33,6 ms.
