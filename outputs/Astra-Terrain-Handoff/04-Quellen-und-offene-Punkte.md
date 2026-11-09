# Quellen, Projektstand und offene Punkte

## Gelesene Projektquellen

- [Großes Repository-README](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/README.md) – Spielwelt, Viertel, Geschäfts- und Alltagssysteme, Tankstelle/Reparatur, Bus und Bahn.
- [Unity-README](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/unity/README.md) – Unity-Prototyp, derzeitiger Projektumfang und nicht portierte Spielsysteme.
- [Bahnhofsviertel-Beschreibung](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/unity/BAHNHOFSVIERTEL.md) – Bahnhofserweiterung östlich der bestehenden Unterführung.
- [Waldsee- und Bergland-Update](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/unity/LANDSCAPE-UPDATE.md) – Gelände, See, Wald, Hügel, Schneegrenze und Wetteranbindung.
- [Terrain-Rendering-Notizen](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/unity/ZeroRise/Assets/ZeroRise/Art/URBAN-RENDERING.md) – native TerrainData, Bodenschichten, Materialvariation, Vegetationsdichte und Leistungsgrenzen.
- [Oberflächen-Prompts aus dem Branch](https://github.com/xen3rgy/GameDevelopment/blob/zero-rise-v0.7.2/asset-prompts-069.txt) – stilisierte, wiederverwendbare Oberflächenmaterialien.

## Wichtige Projektgrenzen

- Das große README beschreibt ein browserbasiertes Spiel mit vier Vierteln und vielen Funktionen.
- `unity/ZeroRise` ist ein separater spielbarer Prototyp. Die README sagt ausdrücklich, dass Ökonomie, Missionen, Survival, Tuning und Browser-Spielstände noch nicht portiert sind.
- Der Terrain-Branch nutzt bereits native Terrain-Daten und eigene Shader. Ein neues Projekt ist nicht automatisch nötig; eine separate Terrain-Testszene im bestehenden Unity-Projekt kann Pipeline und Materialien teilen.
- Editorversionsangabe ist uneinheitlich: Unity-README nennt `6000.6.5f1`, `ProjectVersion.txt` nennt `6000.6.7f1`. Vor einer frischen Terrainproduktion abgleichen.

## Nicht in den übergebenen Referenzen enthalten

Im referenzierten Chat waren vier Screenshots enthalten, aber kein separates Vorab-Konzeptdokument. Falls dieses Dokument an anderer Stelle existiert, muss es noch ergänzt werden. Ebenso fehlen aktuell verbindliche Koordinaten und Grundstücksgrößen für alle Geschäfts-, Hafen-, Verkehrs- und Wohnorte in einer gemeinsamen Unity-Welt.

Bis diese Planung vorliegt, soll Astra keine exakten Standorte, Gebäudegrößen, Straßengeometrien oder Haltestellen erfinden. Die Terrainrunde soll freie, plausible Reservate erhalten und alle unklaren Punkte als Vorschlag markieren.
