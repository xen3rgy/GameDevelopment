# 2D-Kartenansicht – Entwurf für die M-Taste

## So soll sie sich lesen

- Vollbildkarte, exakt von oben, Norden oben. Die Karte zeigt die ganze zusammenhängende Spielwelt; sie ist kein 3D-Screenshot.
- Das bestehende Westbruch-Straßennetz bleibt am westlichen Stadtteil erkennbar. Die Bahntrasse verläuft diagonal durch die Karte und führt zum Bahnhof als zentralem Orientierungspunkt.
- Östlich des Bahnhofs öffnen sich die Stadtblöcke in Wald, See und Bergland. Das Stadtzentrum und die gehobene Wohnlage erhalten eigene, unterscheidbare Flächen.
- Straßen und Hauptwege sind heller und breiter als Nebenwege. Die Bahn erhält eine eigene dunkle Doppellinie. Viertelgrenzen bleiben weich und folgen der tatsächlichen Bebauung.
- Der Spieler wird mit einem klaren Richtungspfeil gezeigt. Bahnhof, Werkstatt, Geschäfte und Waldsee sind über einheitliche Ortsmarken auffindbar.
- Ebenen wie Viertel, Wege, Bahn und Natur sowie Zoomen, Verschieben und Schließen sind direkt bedienbar. Die M-Taste schließt die Karte wieder.

## Orientierung für Astra

`07-karte-im-spiel.svg` ist das konkrete UI- und Farblayout. `01-gesamtkarte.png` zeigt die grobe Weltkomposition, `02` bis `06` zeigen die jeweilige Umgebungswirkung.

Für das ursprüngliche Viertel sind die Straßenpositionen aus `unity/ZeroRise/Assets/ZeroRise/Data/Westbruch.json` maßgeblich: Nordring, Westbruchstraße, Südring, Uferstraße, Hofstraße und Stadtanschluss. Der Bahnhof liegt laut `unity/BAHNHOFSVIERTEL.md` hinter der östlichen Bahnunterführung; Waldsee und Bergland schließen östlich an. Die Konzeptkarte vereinfacht Entfernungen und Straßengeometrie zur Lesbarkeit und ist kein maßstabsgetreuer GPS-Plan.

Die Karte sollte aus den echten Straßen-, Bahn-, POI- und Terrain-Daten der Szene aufgebaut werden. Keine Karte als Screenshot oder Textur fest in die UI einbauen.
