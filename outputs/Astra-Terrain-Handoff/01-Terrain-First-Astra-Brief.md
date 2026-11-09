# Terrain-first-Umsetzungsbrief für Astra

## Ziel der ersten Runde

Erstelle eine zusammenhängende, erkundbare Landschaft für ZERO // RISE, bevor Gebäude oder Stadtobjekte gebaut werden. Das Ergebnis soll schon aus der Spielerperspektive interessant wirken: deutliche Großformen, abwechslungsreiche Bodenschichten, glaubwürdige Ufer, erkennbare Waldkanten und eine lesbare Silhouette am Horizont.

## Inhalt der ersten Szene

**Erlaubt:** native Terrain-Geometrie und Kollision, Erde, Lehm, Kies, Gras, Waldboden, Laub, Moos, Felsen, natürliche Böschungen, Bäume, Sträucher, Farn und andere Bodendecker, Hügel, Bergzüge, Seen, kleine Bachläufe und natürliche Ufer.

**Nicht Teil der ersten Szene:** Gebäude und Gebäude-Platzhalter, Straßen, Gehwege, Pflaster, Schienen, Bahnsteige, Haltestellen, Parkplätze, Zäune, Schilder, Laternen, Bänke, Fahrzeuge, Figuren, Geschäfts- oder Hafenobjekte. Auch Würfel und andere Platzhalterobjekte sind ausgeschlossen.

Künftige Straßen- und Bauflächen dürfen in einer **separaten Lageplan-Grafik** als transparente Konturen oder freie Flächen vorkommen. Sie dürfen in der Terrain-Ansicht selbst nicht als fertige Objekte erscheinen. Reservierte Stadtflächen sollen die spätere Bebauung ermöglichen und dürfen deshalb keine hohen Felsen, Seen oder steilen Hänge enthalten.

## Landschaftsaufbau

- Bestehende Stadtregion Westbruch: überwiegend gut begehbares, sanft modelliertes Terrain. Flache Zonen für die vorhandenen Stadtblöcke und künftige öffentliche Orte freihalten.
- Bahnhofsregion: Gelände darf in einer diagonalen Bahntrassen-Zone leicht erhöht oder eingeschnitten sein. In dieser Runde nur Erde, Böschung und Schottercharakter; keine Schienen oder Bahnsteige.
- Übergänge zur Natur: aus Stadtgrün werden Wiesen, dann lockere Baumgruppen und dichterer Wald. Die Übergänge organisch staffeln statt durch abrupte Biom-Grenzen.
- Waldsee: nachvollziehbares Seebecken mit geschlossenem Uferanschluss, feuchten Senken, Schilf- und Grasrändern sowie einem natürlichen Höhenverlauf zum Wasser.
- Bergland: hinter Wald und See ansteigende Hügel und Felsrücken. Eine Schneegrenze nur dort einsetzen, wo sie in der vorhandenen Landschaftsvision vorgesehen ist.

## Bodensignaturen der Viertel

| Gebiet | Terrain- und Materialwirkung |
|---|---|
| Westbruch / einfaches Wohnviertel | Warme, leicht verdichtete Erde, Lehm, kleine Kiesfelder, trockenere Grasinseln und sichtbare Abnutzung. Nicht verwüstet darstellen. |
| Bahnhofsumfeld | Dunklerer mineralischer Boden und grober Schottercharakter an der freigehaltenen Trasse; feuchtere Erde in Senken. Keine Gleise oder Stadtbeläge in dieser Phase. |
| Waldviertel | Dunkler Humus, Laub, Moos, Wurzeln, Farn und Steine; wechselnde Dichte mit natürlichen Lichtungen. |
| Gehobene Wohnlage | Ruhige, gepflegte Grünflächen, feineres Gras, dunkle nährstoffreiche Erde in Mulden und weich modellierte Oberflächen. Keine Zierbeete oder Gartenmöbel. |
| Stadtzentrum und Geschäfts-/Hafenflächen | Weitgehend ruhige, stabile Höhen und kompaktere Bodenwirkung als Baugrund. Der genaue spätere Belag bleibt ausgespart. |

Diese Viertel unterscheiden sich in Pflegezustand, Bodenmix und Bewuchs. Sie sind keine extrem voneinander getrennten Klimabiome.

## Technische Leitplanken

- Terrain-Farbe nicht aus einer einzelnen großflächigen JPEG-Textur erzeugen. Materialflächen mit passender Grundfarbe, Normalen und Rauheits-/Maskendaten mischen; Wiederholungen durch Weltmaßstab, mehrskalige Variation und organische Übergänge reduzieren.
- Relief und Materialmalerei müssen dieselbe Gelände- und Feuchtigkeitslogik verwenden. Vegetation an Gelände, Hangneigung und Wasserabstand ausrichten.
- Native Terrain-Kollision und begehbare Steigungen vorsehen. Geländeanschlüsse an bestehenden Höhen der Stadt und der späteren Infrastruktur beachten.
- Bäume und Bodendecker in räumlich gebündelten Flächen setzen. Große Wälder nicht aus identischen, gleichmäßig verteilten Instanzen bauen; Baumalter, Lücken und Unterwuchs variieren.
- Performance mit Dichte, Sichtweite und Detailstufen planen. Im Branch dokumentiert die Stadtlandschaft bereits eine hohe Vegetationsdichte und relevante Auflösungskosten.

## Abzuliefernde Bilder/Ansichten für die Terrainphase

1. Eine reine Terrain-Gesamtansicht von oben oder schräg oben: Stadtflächen, freie Korridore, Wald, See und Berge sind erkennbar; keine Objekte.
2. Je ein Spielerperspektiven-Bild für einfaches Wohnviertel, Bahnböschung, Wald-/Uferzone und gehobene Grünlage.
3. Eine Materialtafel mit Nahansichten der Bodenmischungen, ohne Häuser oder Deko.
4. Ein schematischer Höhenplan mit Norden, Wasserlinien, Hängen, Ebenen und Bau-/Verkehrsreservaten.
5. Eine Vegetationsdichte-Übersicht: Waldkern, Waldsaum, Wiese, Uferbewuchs und baumfreie Reservate.
6. Ein Wettervergleich derselben Landschaft bei trockenem Tageslicht und nach Regen, damit Feuchtigkeit und Bodenreaktion definiert sind.

## Abnahme der ersten Terrainrunde

- Das Gelände wirkt aus Spielerhöhe abwechslungsreich und bleibt auf der Übersicht verständlich.
- Es gibt keine sichtbaren Texturkacheln oder abrupten Übergänge zwischen Bodenlagen.
- Ufer, Böschungen und Terrainränder sind geschlossen; Bäume schweben nicht und versinken nicht sichtbar.
- Reservierte Flächen bleiben nutzbar und frei von hohen Geländehindernissen.
- Die Umgebung wirkt wie stilisierte Echtzeit-Spielgrafik, nicht wie ein Landschaftsfoto und nicht wie eine flache Fototextur.
