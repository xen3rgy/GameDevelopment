# ZERO // RISE

A self-contained, browser-based 3D singleplayer life and business game in German.

## Run locally

On Windows, double-click `Spiel-starten.cmd` and open http://127.0.0.1:8765/. Keep the server window open while playing; closing it stops the local website. Alternatively run `npm start` (Node.js, no install step). If the browser reports `ERR_CONNECTION_REFUSED`, start this server again. The launcher also recognizes the bundled Codex Node runtime on this computer.

Serve `dist/` using an HTTP static server. For example, from this checkout:

```sh
python -m http.server 8000 --directory dist
```

Open http://localhost:8000. No API keys, external services or package installation are required. Three.js r170 and the generated runtime assets are committed directly in `dist/`; no restore step is required. A browser with WebGL2 and hardware acceleration is required.

## Version 0.7.5 · Stabilitäts-Pass

- ÖPNV-Ankunftspunkte von Interaktionspunkten getrennt und gegen Stadtgeometrie validiert. Stadtmitte lag zuvor exakt auf einer Straßenlaterne; diese Kollision ist behoben.
- Nach Bus-/Bahnfahrten wird Spieler, Kamera, Interaktionsfokus und Routenstatus vollständig synchronisiert. Zusätzlich sucht die Laufzeit bei dynamischen Hindernissen einen freien Ankunftspunkt in direkter Nähe.
- Fahrplanzeiten runden Anzeige-Werte auf ganze Spielminuten; interne Fließkomma-Zeit bleibt für die Simulation erhalten, ohne lange Dezimalfolgen im UI.
- ÖPNV-Ziele bleiben beim Markieren über Karte/Navigation erhalten und werden nicht mehr vom Delivery-Zielsystem verworfen.
- Westhafen-Haltestelle und Fahrzeug-Parkpunkt wurden räumlich getrennt.
- Stellplatzbelegung ist eindeutig. Alte 0.7.5-Spielstände mit doppelten Garagenplätzen oder veralteten Parkplatz-Reservierungen werden beim Laden automatisch repariert.
- Beim Losfahren wird eine alte Parkplatz-Reservierung gelöscht; belegte Stellplätze können nicht doppelt verwendet werden.
- Gebrauchtwagen zeigen jetzt realistische Kilometerstände im Bereich von ungefähr 38.000–183.000 km statt fälschlich 38–183 km.
- Zusätzliche Regressionstests prüfen alle ÖPNV-Ankunftspunkte, dynamische Spawn-Recovery, Dezimalzeit-Anzeige, Stellplatzmigration und alle markierten Parkausstiege.

## Version 0.7.5 · Mobilität

- Bis zu fünf eigene Fahrzeuge gleichzeitig. Das aktive Fahrzeug bleibt vollständig fahrbar; weitere Fahrzeuge werden im Mobilwerk auf vier markierten Stellplätzen verwaltet und können dort als aktives Fahrzeug geholt werden.
- Neuer täglicher Gebrauchtmarkt mit zustands-, kilometer-, tank- und preisabhängigen Angeboten für Kleinwagen, Lieferwagen und Sportcoupé. Gekaufte Tagesangebote verschwinden aus dem Markt.
- Individueller Fahrzeugzustand mit Kilometerstand, streckenabhängigem Verschleiß und zusätzlichem Aufprallschaden. Verkaufspreise basieren auf tatsächlichem Kaufpreis und aktuellem Zustand.
- Motorfahrzeuge können freiwillig versichert werden. Die Prämie fällt pro Spieltag an; versicherte Fahrzeuge sind dafür deutlich günstiger zu reparieren. Das Stadtrad braucht weder Kraftstoff noch Versicherung.
- Echter Fahrzeugstauraum: Kleinwagen 8 Plätze / 35 kg, Lieferwagen 16 / 90 kg, Sportcoupé 4 / 18 kg. Das Stadtrad erhält einen kleinen Gepäckträger mit 2 Plätzen / 5 kg und bleibt damit im Early Game praktisch.
- Markierte Parkplätze beim Mobilwerk sowie Parkpunkte bei Markt, Bahnhof, Café und Westhafen. Ein ausgestiegenes Fahrzeug kann auf einen nahen Platz sauber eingerastet werden.
- Bahnhof Lindenstadt West ist funktional: Bus 2, Bus 5 und Regionalbahn R1 fahren nach Spielzeit-Fahrplan. Tickets kosten Geld; Warte- und Fahrzeit laufen durch die gemeinsame Simulation und beeinflussen Bedürfnisse, Fristen, Miete und Cafébetrieb.
- Neue Haltestellen Stadtmitte, Westhafen und Lindenpark sind in Welt, Karte und Navigation integriert.
- Save-Schema 3 bleibt erhalten. Alte Spielstände mit einem einzelnen Fahrzeug werden automatisch in das neue Mobilitätssystem übernommen.

## Version 0.7.4 · Core Polish

- Spieler-Kollisionen verwenden kleinere Substeps und testen beide möglichen Slide-Reihenfolgen. Dadurch bleibt tangentiale Bewegung an Gebäudeecken und schmalen Kanten erhalten, ohne durch Hindernisse zu tunneln.
- Ein gemeinsames Interaction-Modul definiert Reichweiten, Prioritäten und HUD-Verben. Arbeitsziele, aufhebbare Gegenstände, Fahrzeuge, Personen und Gebäudeeingänge konkurrieren damit nicht mehr nur nach roher Entfernung.
- Das Interaktions-HUD zeigt jetzt Aktionstyp und Entfernung. HUD und Welt teilen sich den gecachten Fokus statt in kurzen Abständen dieselbe Umgebung doppelt zu durchsuchen.
- Innenraumkamera auf maximal 4 m verdichtet, Kollisionspuffer leicht erhöht und das Ausfahren nach Hindernissen verlangsamt. Einfahren bleibt sofort, damit Wände und Möbel nicht durch die Kamera schneiden.
- Segmentprüfungen für Navigation/Kollisionen wurden dichter abgetastet. Bestehende Save-Schema-3-Spielstände bleiben kompatibel; Wirtschaft, Jobs und Progression werden nicht zurückgesetzt.
- Neue Core-Polish-Regressionstests prüfen Wall-Sliding, Interaction-Priorisierung und Kamera-Recovery. CI läuft zusätzlich direkt auf dem Branch `zero-rise-v0.7.4`.

## Version 0.7.3 · Stadtbild & Wege

- Closed the exposed outer edges of raised pavements and added continuous, walkable building aprons. The station's two granite steps now agree with the physical ground heights instead of protruding into the lower level.
- One properly contained entrance per functional address, with threshold, glazing, handle and house number. Storefront windows and awnings reserve space for the entrance; awning stripes follow their tilted supporting surface.
- Repositioned street trees using a shared visual/collision layout, with tested clearance from lamps, facades, benches and planters. Added slatted benches, lower planted beds and the existing textured tree asset in courtyard tree beds.
- Rebuilt street signs with framed panels and independent, correctly oriented text on both faces; extended them to western crossings. All map labels use aspect-correct, measured text textures without horizontal squeezing.
- Softer paving and cobblestone textures, consistent world-space pavement scale, corrected brick scale on building side walls, textured grass and brighter daytime ambient light. The existing low-quality mode and bounded night-light pool remain in place.
- New map regression checks cover entrance bounds, planting clearance, sign faces, facade UV density, physical frontage support and closed kerbs. Existing save schema 3 is unchanged; no reset is required.

The local preview also provides `/__qa`, a separate map-inspection scene with location, reverse-view, day/night and quality controls. It never loads or writes browser saves. QA/media tools are outside `dist/` and are not part of the shipped game. This is an upgrade to the existing stylized browser world, not a switch to a photorealistic engine.

## Controls

WASD or arrows: camera-relative walking. In vehicles, W/S accelerates or reverses and A/D steers. Move the mouse: free camera look (pointer lock). No held button required. Escape releases the pointer and pauses; resume re-acquires it. Touch: drag the game view. Wheel: zoom. Shift: sprint. Space: jump on foot; brake in a vehicle. 1/2: consume food/drink. E: interact. F: enter/exit a nearby owned vehicle. I: inventory. M: map. Tab: phone. Escape: pause. Touch movement and interaction controls are included.

## Version 0.4.3 · Food scale and night lighting

Replaced oversized bread and produce with metre-authored reusable meshes. The earlier bug overwrote unit-sphere radius scaling when changing proportions; trees and waste bags received the same correction. Added explicit food-bounds regression coverage. Lighting now fades through dusk to full night by 20:00, removes the minimum daytime sun, dims environment lighting outdoors, preserves interior lighting, and uses an eight-light stable local streetlamp pool (four in low quality), warm halos and two local shadow maps on high quality. Expanded lamps to the other district streets. Source versions invalidate stale scene-module imports. No save schema change.

## Version 0.4.2 · Startup hotfix

Renamed the interior module to avoid the client-side filename block reported by the player. Versioned the world import so cached callers cannot keep requesting the old path. The startup module catches dependency loading failures and shows a retry action without touching saved progress.

## Version 0.4.1 · Market interior

Rebuilt the market as a coherent neighborhood grocery: illuminated drinks chillers with glazed doors, a low fresh-food island, labeled grocery packs, bakery, produce crates, shelf prices derived from item data, ceiling panels/LEDs, porcelain floor, glass exit doors and a checkout belt/scanner/POS. Bakery and produce fixtures support the existing basket/checkout flow. All sales points are reachable around the fixture colliders; older interior saves inside new furniture recover at the entrance. No new save schema or reset is required. The card terminal is visual decoration; checkout still uses the existing in-game cash transaction.

## Version 0.4

Visual overhaul: differentiated masonry/stucco architecture with actual window frames, balcony rails, shop awnings and roofs; a generated kingfisher mural; layered tree crowns; more proportionate clothed characters with rigid geometry merged per animation joint; rounded vehicle bodies, sloped glazing, rims and lamps. Moving sky clouds, a less washed-out daylight balance, warm night windows, finer paving and coherent inventory symbols. Adjustable brightness, field of view and mouse sensitivity are saved with existing settings. Existing schema-3 saves remain compatible; missing view settings receive defaults. Camera/ground fixes and physical shopping from 0.3 are retained.

The mural in `dist/assets/lindenstadt-mural.png` was generated specifically for this project. City geometry, shader effects and interface symbols are source-authored. No external runtime asset hosts are used.

## Implemented

- Structured asphalt and paving, specular puddles, gradient day/night sky, nearby working streetlights, facade details, outdoor seating and smoother articulated characters.
- Procedural 3D town with four districts, collision, follow camera, walking pedestrians, traffic, a daylight cycle and rain.
- Story start without money; separate sandbox start with €20,000.
- Eleven connected progression objectives, experience, reputation, and four skill tracks.
- Capacity- and weight-limited stack inventory, consumables, split and drop/pickup.
- Bottle collection, daily respawns, return machine and survival needs.
- Walkable MARKT 24 interior with stocked shelves, colliding fixtures, cashier, unpaid basket and atomic checkout. Exit returns unpaid goods; saves preserve an in-progress shopping trip.
- Visible apartment and market exit doors; continuous camera collision and surface-specific foot placement.
- Physical delivery, street-cleaning and warehouse jobs.
- Shelter, park rest, three rental housing tiers and a furnished walkable home interior with tier-specific additional furniture.
- Home storage (32 slots / 100 kg), atomic transfers, four cooking ingredients and two recipes using combined backpack/home supplies.
- Bike and three motor vehicles, acceleration, steering, braking, persistent heading and parking, fuel, condition, service and sale.
- Two businesses with stock, staff, pricing, quality, marketing, daily costs, income and economic cycles.
- Education, three persistent personal contacts, bank transfers, loans and income properties.
- Pedestrian A* routes around building colliders, nearby interaction focus, smoother walking, substepped collision, indoor camera bounds and guarded job completion.
- Menus and losing focus pause simulation; auto-save and manual save to device-local storage, JSON export/import with version migration, validation and recovery from a local backup.
- Instanced static geometry, selectable rendering quality and synthesized interaction/footstep sounds.

## Implementation boundaries

This is a playable browser release, not a finished commercial PC game or a Unity project. Geometry and characters are procedural and stylized. Housing tiers share one interior shell with different additional furnishings. Ambient pedestrians and traffic follow simple routes; contacts have fixed meeting positions. Business staff are a simulated team count rather than individual scheduled NPCs. Income properties are aggregate investments, not individually furnished buildings. Banking and saves are local to the browser; no real money or external accounts are involved. Romance, generations, multiplayer, full character customization, manufacturing chains and a stock market are not implemented.

## Architecture

- `dist/food-models.js`: metre-scaled bread and produce meshes with reusable geometry.
- `dist/lighting.js`: time-of-day lighting and stable streetlamp selection.
- `dist/shop-interior.js`: market interior, product packaging, sales displays and lighting.
- `dist/art.js`: architecture, vehicles, vegetation and character rendering, including per-joint mesh batching.
- `dist/icons.js`: shared inventory symbols.
- `dist/data.js`: content definitions and progression.
- `dist/model.js`: serializable simulation, inventory and transaction rules, import validation.
- `dist/world.js`: Three.js world, movement, collision, vehicles, dynamic objects and camera.
- `dist/app.js`: game orchestration, input, HUD, map, menus and local save lifecycle.
- `dist/spatial.js`: ground heights, room/fixture collision and continuous camera rig.
- `dist/movement.js`: pure character and vehicle collision/locomotion rules.
- `dist/navigation.js`: cached city navigation graph and collision-safe path smoothing.
- `dist/atmosphere.js`: surface textures, sky, reflections, wetness and local street lighting.
- `dist/inventory.js`: reusable atomic inventory and transfer rules.
- `dist/persistence.js`: device-local primary/backup save lifecycle.
- `dist/style.css`: responsive game interface.
- `tests/model.test.mjs`: state and economy regression tests, including full objective progression.

Run `node --test tests/*.test.mjs`. The tests exercise simulation and persistence; they do not substitute for an interactive browser playthrough.

## Saves

Save schema version 3, with automatic migration from versions 1 and 2; unchanged localStorage key `zero-rise-save-v1`. Save files contain simulation time, cash and accounts, inventory, needs, housing, parked vehicle, active job, contacts, businesses, world pickups/drops, objectives and settings. The `.backup` key retains the previous valid local snapshot. Future breaking schema changes must introduce migration. A manual JSON export is the portable backup.

### 0.4.4 — Readable night streets
- Raised nocturnal hemisphere/environment fill and added cool moonlight without daylight or sky brightening; dusk timing unchanged.
- Denser streetlight coverage, broader warm pools, brighter actual projectors below non-shadow-casting bulbs.
- Four pooled vehicle projectors follow the nearest moving cars (two on low quality), prioritize the driven vehicle, and aim at the road. Head/tail lens emission follows dusk.
- Save format and indoor lighting unchanged.

### 0.5.0 — Café Morgen

- Café Morgen is now a furnished, walkable interior with a counter, coffee machines, pastry display, menu board, three table areas, plants, warm lights and an operating book.
- Owners can run one hands-on shift per game day: guests enter, order, wait, eat and leave; the player takes orders, prepares one of three recipes, collects the tray, serves and clears tables.
- The shift simulation tracks patience, missed guests, service count, ingredient costs, revenue and tips. Staff automatically clear used tables; price, quality, marketing and the economy influence the live shift.
- Hands-on shift results are posted exactly once at midnight and replace the café's automatic daily simulation for that day. Leaving, exhaustion, midnight and the timer close an active shift safely.
- Existing version-3 saves gain a validated café state without losing progress. Active shifts, guests, prepared orders and reports survive saving and loading.

### 0.4.6 — Sidewalk consideration and sound controls

- Reciprocal right-hand NPC passing, finite turn rate, body separation and distance-driven steps. Standing contacts and the player are included; blocked movement does not advance the walk cycle.
- Separate 0–100% environment, vehicle and effect buses in the pause menu. Existing master mute remains. Environment one-shots, rain, fridge hum and engines use the appropriate buses; values persist in the existing save format.
- Walking collision with the owned parked vehicle, including rotated cars, bike and van. Driving excludes self-collision; overlapping legacy saves can walk out.
- Existing camera behavior retained after regression checks. No browser visual/audio QA or FPS measurement was available; automated model, simulation and sound-graph tests validate behavior.

### 0.4.5 — City life

Hotfix: NPCs use a shorter, foot-planted IK stride and normal walking speeds, slowing during turns. Traffic uses circular pedestrian clearance and motion prediction; approaching crossings reserve only the near lane, not both lanes for stationary bystanders. No save-format changes.
- Browser-native procedural soundscape: stereo traffic engine voices, filtered rain and ambience, indoor fridge/room hum, shopping/door effects and six ground-specific footstep profiles. Starts on user gesture, follows existing sound toggle, fades during menus/blur and stops updating in hidden tabs.
- Distance-driven procedural gait with knee/elbow joints, stable feet, upper-body motion, blended stopping, pickup/reach gestures, held shopping basket, moving driver door and 0.85-second entry/exit presentation. Vehicle state remains immediately saveable; control locks during the visual transition.
- Seven continuous lane routes with curved turns, bounded acceleration/braking, obstacle checks, crossing reservation and serialized junction priority. Traffic headlights follow existing cars; brake lights and wheel rotation added.
- Camera angle-wrap smoothing and eased zoom; shallower indoor orbit. Exterior/home instance pools separated so inactive worlds and exterior lights can be hidden; nearby pedestrian animation only, 30 Hz shadow refresh, throttled light selection and focus queries.
- Save schema remains 3. Audio and animations are procedural, not recorded performances or motion capture. Browser visual/audio QA is unavailable in this static-site environment; math/model/scene-graph checks cover the new behavior.

### 0.6.0 — Delivery contracts and addresses

- Four delivery contracts: one parcel (32 €), two-stop canal round (58 €), unlocked three-stop city round (85 €) and express run (70 € plus 20 € deadline bonus). Logistics pay is quoted at pickup; a used delivery van adds 25% of the quoted base.
- New contracts require physical pickup at Kiez & Kurier, one inventory slot and 2 kg per package, individual handoffs in the listed order, and final settlement only after all stops. Aborting returns every outstanding package without payment.
- A saved courier career starts at 60/100 reliability: completed tours add up to 5, late express tours subtract up to 8, cancellation up to 6. Larger contracts require 2 completions/65 reliability; express requires 4/75. Old job history is preserved without inventing a courier track record.
- The 330-game-minute express window spans midnight and follows world speed, sleep and other time advances. Menus pause it. Late deliveries retain base and earned van pay; the time bonus is lost.
- Stop list and latest itemized receipt are accessible through Phone → Jobs and the active-job HUD. Numbered map markers advance with handoffs. Matching street names, house numbers, facade details, signs and walking-route directions support orientation.
- Existing schema-3 saves and legacy single-parcel jobs remain loadable. Added contract integrity checks and 11 automated tests; 132 tests pass in total. No rendered browser playthrough or performance measurement was performed for this release.
- New modules: `contracts.js` (rules), `courier-ui.js` (paperwork), `orientation.js` (shared addresses/directions), `city-addresses.js` (facades and signposts).

### 0.6.1 — Shared clock and café staff

- One real second advances one game minute at 1x; 4x advances four. The common model update drives needs, café simulation and delivery deadlines in all locations. Menus and hidden tabs still pause.
- Café opening window: 08:00–20:00. A personal shift lasts up to 210 game minutes and is capped at closing. The HUD and operating book show availability and scheduled close. Old elapsed-time accounting is migrated without rewriting historical revenue.
- Staff enter through the hinged doorway, walk to their stations, then admit customers. On personal shift end they walk out and disappear. Commutes persist across saves.
- Upgrades increase work rate to 1.5x / 2.1x and walking speed to 1.65 / 2.15 m/s (base 1.15). Old training purchases remain. Service chooses older waiting orders ahead of payment collection, delivers ready food first, and stops unnecessary home-return trips. Paths use fixture visibility and exact segment collision checks.
- New sandbox games start with 20,000 euros. All contracts, including express, are available immediately in sandbox; story unlock requirements remain. New express jobs use a 105-game-minute deadline; saved old express jobs retain 330 minutes.
- 140 automated tests cover shared time, acceleration, migration, commutes, collision paths, cash, contract deadlines, and comparative seeded staff performance. No browser visual playthrough has been performed.


## 0.6.2 — Café-Betrieb und 10×

- Spieltempo 1×/4×/10×; 10× entspricht 144 echten Sekunden pro Spieltag. Zeitüberspringen simuliert denselben Cafébetrieb wie laufendes Spiel.
- Neue Cafébetriebstage dauern höchstens 480 Spielminuten, spätestens bis 20:00; Start 08:00–18:00, keine neuen Gäste in der letzten Stunde.
- Mindestens Barista + Service lassen den laufenden Betrieb während der Abwesenheit fortsetzen. Ohne Abräumkraft müssen Plätze weiterhin vom Spieler gereinigt werden. Ohne ausreichendes Team schließt das Café beim Verlassen. Jeden Betriebstag bewusst im Betriebsbuch starten.
- Nur tatsächlich bediente Gäste erzeugen Umsatz. Der pauschale Restbetrieb entfällt für neue Schichten. Trainingsstufen wirken innen und außen identisch.
- Barista kann zubereiten, während der Service ein anderes Tablett trägt. Auslieferung, Abholung, Verwerfen und Speichern halten die beiden Bestellungen getrennt.
- Spielökonomie: 3,75 € je Einsatzstunde/Person, mindestens zwei Stunden; 24,50 € Tagesfixkosten; optional 15 € Werbung. Ein Vorratstag ist Verbrauchsmaterial (8,33 € als gerundeter Packanteil), keine Zutatenpauschale. Zutaten werden beim Beginn einer Zubereitung erfasst, einschließlich verworfener Mahlzeiten.
- Frühstück 8,90 € bei 100 % Preisniveau, 2,60 € Wareneinsatz. Qualität, Marketing, Wirtschaftslage, Preiselastizität und Unternehmenswissen beeinflussen echte Ankünfte; Wissensbonus ist auf +40 % Nachfrage begrenzt.
- Schichtgewinn enthält sämtliche Tageskosten; die Mitternachtsbuchung addiert vorausbezahltes Verbrauchsmaterial zurück, damit es nicht doppelt vom Bargeld abgezogen wird. Einstellungen und Training sind Investitionen außerhalb des Betriebsergebnisses. Tage ohne Öffnung bringen keinen Umsatz, verbrauchen keinen Vorrat und kosten nur Fixkosten/aktivierte Werbung.
- Bestehende Kassen, Rechnungen, Teamstufen und Fortschritte bleiben erhalten. Bereits gespeicherte ältere Schichten behalten ihre Dauer und bisherige Tagesabrechnung, bis ein neuer Betriebstag startet.
- Verifiziert: 147 automatisierte Tests, darunter 10×/Zeitsprung-Gleichheit, Abwesenheit, Tagesabrechnung, Mindestlöhne, parallel getragene Mahlzeiten, Migration und 64 deterministische Wirtschaftsläufe. Kein neuer manueller WebGL-Spieldurchlauf.


## 0.6.3 — Persönlich zugestellt

- Sechs Verträge mit Filtern für entspannte und Express-Touren; zusätzlich einzelne Kontor-Lieferung und früh freischaltbarer Kapitel-Express. Auswahl im Smartphone, persönliche Abholung bei Kiez & Kurier.
- Transportwahl zu Fuß, Stadtrad oder Auto/Lieferwagen. Fahrzeuge müssen fahrbereit und in der Nähe der Paketausgabe geparkt sein. Vorschau zeigt Route, Strecke, Gewicht, erwartete reale Dauer und Auszahlung.
- Neue Expressfristen beruhen auf der Wegstrecke, Transportart, Übergabezeiten und einer Reserve. Die Frist wird beim Ende der Paketübernahme gestartet. 1×, 4× und 10× werden bei Annahme berücksichtigt; ein späterer Tempowechsel ändert die vereinbarte Spieluhr-Frist nicht. HUD warnt sichtbar und zeigt reale Restzeit.
- Vier persönliche Annahmestellen mit Namen, Schildern, Paketablagen und Zielringen. Stadtgeometrie bleibt erhalten. Navigationsziele liegen vor den Empfängern.
- Paketübernahme (2,2 echte Sekunden), zweihändiges Tragen und Empfängerübergabe (2,6 Sekunden) mit Arm-IK. Die Spielfigur hält für die Übergabe an, die Kamera bleibt bedienbar. Im Fahrzeug werden Pakete verstaut. Abschluss, Inventar und Zahlung folgen erst nach beendeter Übergabe; Pausen und Speichern erhalten den Ablauf.
- Lieferwagenbonus anteilig nach tatsächlich gefahrener Strecke; kein voller Bonus für kurzes Anfahren. Abrechnung trennt Grundbetrag, Expressbonus, Wagenbonus und entgangenen Zeitbonus. Kraftstoff/Verschleiß sind geschätzte Kosten zur Orientierung, keine zusätzliche Abbuchung.
- Bestehende aktive Lieferaufträge behalten ihre alten Fristen und Regeln. Neue Aufträge erhalten Protokoll 2 mit validiertem Routenplan und resumierbaren Übergaben. Café, Fortschritt und Kartenumfang bleiben erhalten.
- Verifiziert: 155 automatisierte Tests einschließlich aller sechs Touren bei 1×/4×/10×, Speichern mitten in Übergaben, Abbruch, Einmalzahlung, Fristwechsel, Bonus, Migration und erreichbarer Annahmestellen. Kein manueller WebGL-Spieldurchlauf möglich; visuelle Qualität wurde nicht im Browser überprüft.


### 0.6.3.1 — Auswahl und Tragetempo

- Auswahlbuttons behalten ihre dunkle Grundgestaltung. Goldener Rahmen, kontrastreiche Schrift und Häkchen kennzeichnen aktive Transport-/Auftragsfilter; aria-pressed und Tastaturfokus sind gesetzt.
- Gemeinsames Bewegungsprofil: Pakete 2,8 m/s statt 4,25 m/s, Lagerkisten 2,45 m/s, Café-Tablett 2,65 m/s. Mit sichtbarer Last kein Sprint oder Sprung; bei Erschöpfung höchstens 2,25 m/s. Im Fahrzeug verstaute Pakete begrenzen die Fahrt nicht. Ohne Last bleiben die bisherigen Geschwindigkeiten erhalten.
- Kürzere, weiterhin streckenabhängige Schritte beim Tragen; sanfteres Beschleunigen. Neue Fußtouren planen mit 2,55 m/s plus Reserve. Bestehende Verträge behalten ihre vereinbarte Frist.
- Prüfung: echte Auftragsannahme/Abbruch, Lastwechsel, 1×/4×/10×, Fahrzeug/Innenräume und alle sechs Fußtouren bei erschöpftem Tragetempo. Kein visueller Browserdurchlauf verfügbar.


## 0.6.4 — Zuhause & Alltag

- Bett: 2/4/6/8/10 Stunden Schlaf mit Start-/Aufwachzeit und konkreten Warnungen für Expressfristen, Cafébetrieb, Mitternacht und knappes Essen/Trinken. Schlaf und andere längere Aktionen sind explizite Zeitsprünge mit sichtbarer Animation; normale Zeitfaktoren bleiben 1×/4×/10×.
- Erholung läuft während der tatsächlichen Schlafzeit auf. Einfaches Bett: 0,14 Energie je Spielminute, bequemes Bett: 0,17. Bei Hunger oder Durstvorrat unter 15 nur 60 % Erholung; alle Bedürfnisse maximal 100. Keine automatische volle Heilung. Lange Wachzeiten steigern die Belastung beim Sprinten bis +65 %; starke Übermüdung verhindert Sprinten. Kaffee erhöht Energie, setzt Wachzeit aber nicht zurück.
- Neuer Kühlschrank (12 Plätze / 20 kg), nur für Nahrung, Getränke und Zutaten. Bestehendes Wohnungslager bleibt separat erhalten. Küche nutzt alle drei Inventare atomar; fertiges Essen bevorzugt Rucksack, dann Kühlschrank, dann Lager.
- Sichtbare Schlaf-/Aufstehpose, Duschsichtschutz und Wasser, Kochgeste, Wasserflasche/Kaffeetasse/Sandwich/Mahlzeit mit Besteck. Kühlschranktür und sichtbare Vorräte folgen dem Menü-/Lagerzustand. Alltagsaktionen sperren Bewegung und andere verändernde Aktionen; Kamera und Pausenmenü bleiben nutzbar.
- Verbrauch erfolgt erst am Ende einer Ess-/Trinkanimation. Abbrechen verbraucht kein Item; bereits vergangene Ruhezeit und bei Beginn bezahlte Unterkunft bleiben bestehen. Speichern und Laden setzen die Aktion mit ihrem Fortschritt fort. Bereits geschaffte Erholung wird nicht doppelt gewährt.
- Zeitsprünge nutzen dieselbe gemeinsame Simulation wie normale Zeitfortschritte. Viertelminuten-Schritte verhindern unterschiedliche Caféergebnisse durch Bildraten oder die Aufteilung der Schlafanimation. Einnahmen, Betriebsschluss, Miete und Fristen bleiben gekoppelt.
- Kontextuelle Hinweise für Last, Müdigkeit, Hunger/Durst sowie nahe Öffnungszeiten. Kursbesuche erhalten eine Zeitvorschau. Wohnung mit Möbelkollisionen je Wohnungsstufe, freigehaltenen Interaktionspunkten, Kühlschrank, Nachttischlicht und korrigierter Bodenhöhe.
- Bestehende Schema-3-Spielstände werden ergänzt, ohne Vorräte/Geld/Fortschritt umzuschreiben. Neue Zustände und Aktionen werden beim Import geprüft und sichtbare Aktionsnamen aus gültigen Aktionen neu aufgebaut.
- 167 automatisierte Tests: Schlafdauer über Mitternacht bei allen Geschwindigkeiten, Pause/Laden/Abbruch, Versorgung, Müdigkeit, Kühlschrank und Kochen, Verbrauch genau einmal, alle Wohnungswege, Posen/Objekte sowie Übereinstimmung des Cafébetriebs und seiner Tagesabrechnung während Schlaf.
- Kein visueller WebGL-Browserdurchlauf verfügbar. Die Karte wurde nicht erweitert.


## 0.6.5 — Bahnhofsviertel

- First connected map expansion west of the original city: three street links, Westbogen loop, station forecourt, brick viaduct and Gleishof courtyard. Shared footprint excludes unused outer corners and detached interiors.
- Distinct brick facades, station clock driven by the common game minute, kiosk news shelves, workshop shutter, warm windows, planters and benches. Eight nearby lamp slots remain the high-quality budget; the bridge and canopy use elevated camera volumes without blocking the street below.
- Two new recipients (Yusuf/Tessa), three contracts (Morgenausgabe, gemischte Westrunde, Ersatzteil-Express). Existing contracts remain intact. Walking routes avoid buildings and fixtures; delivery deadlines account for route length, selected mode and 1×/4×/10×. Two additional circulating cars and eight sidewalk residents.
- Gleishöfe room: €85 entry and €7.50 every three days, basic bed, kitchen, shower and storage. Housing previews compare actual walkable distances to café and market. Paid moves, including downsizing, require visiting the new entrance and confirming the cost. No deposit refund; inventory, storage and fridge are preserved.
- Home entry/exit, map position and home navigation resolve the owned housing location. New district positions and vehicles survive save/import; existing saves and active jobs keep their identifiers and progression.
- Full map fits the expanded bounds, shows the viaduct and new named destinations. Forecourt and road surface heights match foot placement and footstep categories.
- Validation: 175 automated tests, including expanded navigation and traffic coverage, save roundtrips, moves, geometry, camera clearance, lighting budget and deadlines. No visual WebGL browser playthrough was available. Station platform access and rail transport are not implemented.


## 0.6.6 — Stadtbild & Straßen

- Replaced overlapping road/sidewalk strips with a shared non-overlapping street surface. All road directions and intersections use a single height; 16 cm kerbs remain at the roadside only.
- Crossings share their positions with traffic anticipation, central dropped kerbs and flared ramps. Ramp crease geometry is split analytically; rendered triangles agree with support heights, including corners. Road markings stop before junctions, drains sit at the lane edge, and puddles follow the actual surface height.
- Absolute-height jump integration preserves the ballistic trajectory across changes in the ground below. The camera receives the same continuous player root. Landing resolves against the destination surface; held jump cannot retrigger, menus pause flight, carrying restrictions remain, and room/vehicle/action transitions reset flight state.
- Modern central business buildings retain the original functional footprints and addresses, with glazing, light cladding, fins, canopies and roof pavilions. Western unnamed housing now has detailed street-facing windows instead of blank walls; masonry, smaller paving and framed lamps distinguish the district. Peripheral scenery closes exposed city views without expanding playable boundaries.
- Gleishof wall plaque stays inside the housing facade, viaduct signs have backing and brackets. Lettered map markers match the location list; map surfaces distinguish districts, pavements, roadways and park.
- Regression tests cover flat junctions, non-overlapping surfaces, raycast/physics agreement on every ramp, two-axis crossing priority, jump continuity at 20/30/60/120 Hz, landing/held inputs, architectural footprints and sign placement. Existing economy, delivery, housing and save tests remain in the suite.
- User screenshots and extracted video frames were inspected. No live visual WebGL/browser playthrough was available in this environment; scene geometry and interaction logic were verified programmatically.

## 0.6.7 — Lebendigere Viertel

- Rebuilt peripheral scenery with a continuous, disjoint ground apron around the existing map bases. Foundations intersect the soil; no almost-coplanar ground overlay is introduced. Replaced the old northern tower row with lower western housing and staggered modern eastern buildings. All four facades have windows, with shared night-responsive materials, roof details and closed gables. Layered tree belts and low planting connect the horizon to the city.
- Centered every zebra crossing: nine evenly spaced stripes and equal 54 cm kerb margins on each side. Existing ramp geometry and traffic yielding stay shared with the street layout.
- Added 40 curated fixtures: planted beds, pocket-garden trees, timber benches, bicycle parking, litter bins and a neighborhood noticeboard. Small mailbox and ventilation details sit against existing facades. Physical fixtures share their extents across rendering, collision, camera and delivery planning; functional entrances remain clear.
- Eight additional pedestrians use the northern and southern pavements, offset from existing trees and lamp poles. Walking animation remains distance-driven. Static primitive details use the existing instance batching; no additional realtime lights were added.
- 187 automated checks pass, including actual crossing mesh symmetry, scenery grounding, non-overlapping apron geometry, fixture extents, all location routes and existing gameplay/save regressions. JavaScript syntax checked separately. No live WebGL visual playthrough or device performance measurement was available; screenshots from the user informed the changes.
- Existing saves and business/delivery progression remain compatible. Local module cache versions and the release panel are updated together.

## 0.6.8 — Stadtleben & Feinschliff

Nachfolgekorrektur: **0.6.8.1** (Paketversion `0.6.8-1`).

### 0.6.8.1 — Passanten-Korrektur

- Reproduziert: grobe Wegproben übersahen kurze blockierte Abschnitte an Ecken; winzige Bewegungen setzten den bisherigen Stillstandszähler zurück. In längeren Simulationen kamen zusätzlich gegenseitige Blockaden mit stehenden Autos hinzu.
- Feinere Navigation mit konsistentem Körperabstand, dichter geprüften Wegsegmenten und kontrollierten Richtungswechseln. Wegpunkte werden nur bei freier Anschlussstrecke abgekürzt. Gartenwege sind an den Sitzplatz-Zugängen leicht verbreitert und verwenden weiterhin dieselben Flächen für Darstellung, Bodenhöhe und Navigation.
- Fortschritt wird am verbleibenden Weg gemessen. Bei Blockaden werden Personen und stehende Fahrzeuge bei der Neuplanung berücksichtigt; nach wiederholtem Scheitern wird ein anderes erreichbares Ziel gewählt.
- In Engstellen gibt es feste Vortrittsregeln und freie Ausweichstellen. Der andere Passant wartet, bis das Zurücktreten abgeschlossen ist. Keine Teleports oder sichtbaren Neustarts von Figuren.
- Belegte Aktivitäten können nicht mehr sämtliche Bewohner dauerhaft binden: Bewohner kehren bei fehlenden freien Zielen nach Hause zurück und geben Reservierungen frei. An besetzten Türen wird nach kurzer Wartezeit neu geplant.
- Fahrzeugvorhersagen folgen dem tatsächlichen Kurvenverlauf. Verkehr prüft zusätzlich die konkrete nächste Fahrzeugposition gegen Fußgänger.
- Verifiziert: **198 automatisierte Tests bestanden**, einschließlich einer 30-Minuten-Simulation mit 32 Bewohnern und 9 Autos, enger Begegnung mit Ausweichstelle, Hindernissen durch stehende Personen/Fahrzeuge und dicht abgetasteten Wegsegmenten. Die Langzeitsimulation prüft Körperabstände, Fahrzeugkontakte, sichtbare Positionssprünge und Stillstände von mehr als 25 Sekunden in Bewegungsphasen.
- Bestehende Spielstände und Wirtschaftssysteme bleiben kompatibel. Kein visueller WebGL-Spieltest oder Endgeräte-FPS-Test in dieser Umgebung durchgeführt.

### Ursprünglicher Umfang 0.6.8

- Replaced the ambient lane-loop walkers with a 32-resident population: destinations, reserved bench seats, browsing, station waiting, shop visits and home entrances. Opening times and individual schedules reduce night activity without deleting people in the street. Occasional companions prefer the same destination cluster; waiting for companions is bounded to avoid permanent pair deadlocks. Ambient residents do not create cafe revenue or replace the cafe's own guest simulation.
- Pavement-only routes use a precomputed graph with cached edges, connectivity checks and heap-based A*. Lane crossings are restricted to marked crossing ribbons. Dynamic steering keeps right, slows for turns, avoids people and physical street furniture, and replans blocked trips. Standing pedestrian intention is separate from actual body velocity: cars already on a crossing can clear it, while following traffic yields.
- Sitting now uses an explicit align/backstep/lower sequence and the reverse stand/step-away sequence. Feet use a two-bone solve that accounts for body scale and seat height. A real-distance reverse walk cycle supports the backward step. Bench heights were normalized, one bench was moved away from an overlapping tree, and the shelter seat was moved clear of its back panel.
- Added clothing, trousers, hair, skin tone, body scale, coat, bag and idle head-look variations for ambient residents. Existing player and cafe rig defaults remain unchanged. Only visible nearby resident rigs are animated; resident meshes hide indoors. No new real-time lights were added.
- The cafe-to-station section gains recessed entrance bays, coffee window displays, historically appropriate small facade fittings and garden connections. Disjoint garden path tiles are clipped out of road/pavement footprints, share support heights with movement, and use paving footsteps. Formerly decorative street furniture is solid for walking/navigation; old walking saves inside a newly solid object receive a nearby clear spawn during initial load.
- Validation: 193 automated tests pass, including five minutes of coupled pedestrian/traffic simulation, no body overlaps/visible position jumps/car contact in that scenario, activity transitions, marked-route connectivity, scaled seated feet, day/night scheduling, pause behavior, path support heights, save recovery, and the existing gameplay regression suite. All local JavaScript modules pass syntax checks. These checks are not a live browser/WebGL visual playthrough or a device frame-rate measurement; neither was performed in this environment.


## 0.6.9 — Zwei Gesichter einer Stadt

- Generated brick and limestone surface assets differentiate the historic station district and modern downtown. Added contained display bays, repaired render, downpipes, a station timetable and localized litter. Assets are bundled locally; prompts are in `asset-prompts-069.txt`.
- Added the Lichthof between the bank and mobility store: connected paving, a pergola, planted beds, wood seating and small evening fixtures. Camera overhead clearance, collision, delivery navigation and old-save relocation share the same geometry descriptors.
- Added two sheltered sleeping areas beneath the viaduct, personal belongings, wire trolleys and two seated residents with subtle idle movement. These are ambient scenery, with no new interaction or economy system.
- Road and rail scenery continues into grounded surroundings. Background blocks and planting leave these corridors clear. Existing roads continue through their former pavement caps at the boundaries; masonry becomes closed service gates where the western streets leave the playable area. Playable map bounds remain unchanged.
- Lamp color and output vary by district. Storefront, station canopy and courtyard fixtures share the existing eight-light pool (four at low quality), with at most two shadow-casting street lights. New texture variants share image sources; static props use the existing instance batching.
- Save format, finances, business rules and 0.6.8.1 pedestrian recovery are preserved. Automated geometry, lighting, navigation and gameplay checks run in Node; no live browser/WebGL visual playthrough or device frame-rate measurement was performed.

Validation for 0.6.9: the full 202-test suite passed; after the final boundary-road correction, all 32 affected geometry/lighting/navigation checks and all 11 pedestrian checks passed again, including the 30-minute coupled traffic simulation. The added road-end regression brings the suite to 203 distinct tests. Syntax and relative imports validated for all 56 local JavaScript modules.


## 0.6.9.1 — Stadtalltag

- Connected the Lichthof benches to the existing resident simulation: four individually reservable seats, correctly rotated approaches and seat heights, available 07:30–21:00. No additional population or per-frame pathfinding.
- Residents can hold a takeaway coffee and sip, or glance at a phone during pauses. Gestures use the existing real-time pose updates, finish before departure at each game speed and freeze on pause. A local-coordinate arm solver keeps hand contact across different height/width scales; IK axes are cleared before returning to walking.
- Reserve part of the finite night-light pool for nearby street lamps, so new storefront accent lights cannot consume all road coverage. Out-of-range accent lights are skipped; existing quality budgets remain unchanged.
- Show local landmark names in the existing HUD without additional floating labels. Existing saves and economic systems are unchanged.
- Validation: new contact/animation, rotated-seat navigation, departure timing and light-allocation checks plus existing pedestrian/traffic regression checks. No visual WebGL browser playthrough was performed; this static project has no compatible managed preview.

0.6.9.1 verification: 33 targeted and regression checks passed, including the 30-minute resident/traffic run. All 57 local JavaScript modules pass syntax and relative-import checks.

## 0.7.0 — Die erste richtige Arbeit

Werkstatt West at Gleishof 8 is now an enterable workplace. Follow **Smartphone → Aufträge → Weitere Arbeit im Viertel → Werkstatt markieren** or use the map. Entry is available 08:00–19:00; Tessa accepts new work until 17:00. Existing courier deliveries still take precedence at the exterior recipient, so delivery tours are preserved. New games begin at 08:00; saved games resume at their saved time.

- Three complete physical workflows: receiving stock (count, record discrepancy, select correct bin, reconcile actual quantity); bicycle brake check (diagnose, fetch the 5 mm hex key, adjust, test); tube replacement (identify wheel and size, select part, inspect casing, fit, safety check). Task cards expose the evidence needed for every decision; wrong choices never silently advance the job.
- Separate workshop progress lives inside the existing v3 save, with migration defaults for older saves. Work resumes after saving mid-animation. Courier/warehouse jobs and workshop jobs cannot overlap. Existing staffed café operation can continue on the same game clock.
- Actual operation durations are 36, 51 and 76 game minutes before training, plus player travel time. 1×, 4× and 10× affect both the clock and the operations equally; menus pause both. Movement remains in real metres per second. Parts and wheels visibly occupy the hands, reduce movement to 2.45 m/s, and prevent sprinting/jumping until placed.
- Base wages are €7.50 / €11.00 / €16.50 per accepted job. At most four jobs per game day, including returned jobs. Mistakes require another attempt, remove the quality bonus and deduct €0.75 each, capped at 35% of base pay. Flawless bonus starts at 10% and caps at 20%. Payment is atomic at the desk only after all stages; expired and returned jobs pay zero.
- Courses cost €60 / €140, require 40 / 120 practice XP, and take 90 / 120 game minutes. The first unlocks tube replacement; each reduces operation time by eight percentage points. Course confirmation previews elapsed time and cost. XP is awarded only for accepted completed work.
- Interior: concrete floor, textured brick back wall, fitted steel/wood benches, labelled parts bins, pegboard tools, repair stand, detailed bicycle, service desk and Tessa. Static fixtures are batched; room-local lights and props hide outside. A shared geometry layout controls collision, camera clearance and action proximity.
- Work gestures use attached tools, a clipboard and pressure gauge. Carrying uses the existing two-hand IK. The current work card and live progress are visible while working, and the ordinary city navigation points back to the workshop when away.

Validation: dedicated workflow, wrong-choice, payment, training, time-speed, expiry, collapse, legacy-save, reload, geometry, hand-contact and movement checks. Managed preview has no compatible development server for this buildless static project, so this release was not visually playtested in a browser here.

0.7.0 verification: all 220 automated tests passed, including the 30-minute coupled pedestrian/traffic simulation; the final workshop checks passed again after tool-asset refinement. Syntax and relative imports passed for all 61 local modules.


## 0.7.1 — Handwerk & Teilefahrten

- Revisioned work cards retain all in-progress 0.7.0 jobs. Newly accepted jobs select one of twelve saved variants: 26/28-inch tubes or brake-pad stock with actual counts; four bicycle-check diagnoses; front/rear tube changes with glass, rim-band or valve damage. City, trekking and mountain-bike cards specify compatible sizes and pressures. Never reroll a saved card.
- Customers arrive individually, bring a grounded bicycle, wait for Tessa at reception and return to collect finished bicycles. A bounded queue holds three bikes; opening hours and busyness determine arrivals. Tessa returns to reception when needed and walks to the work area for inventory checks. Ambient customers do not mint player wages. Customer actors are reused, and wheel/frame geometry is shared while bicycle paint is independent.
- Work animation adds hinged carton flaps and matching package counts, wheel removal/replacement on the correct side, a lowered posture with planted feet, hand-to-wheel/handlebar IK and a moving pump handle. Tools and carried parts are hidden outside the appropriate state; the parts-trip carton remains visible outdoors. Parking collision and camera obstacles use the same fixture dimensions.
- A voluntary tube-repair chain adds a depot pickup at Westhafen and delivery to the workshop intake. The base trip wage is €8 plus a €2 punctuality bonus, paid atomically and separately from repair acceptance. Missing the bonus deadline preserves the trip base wage. Cancelling/expiring the repair cannot repay the trip; one chain occupies one of four daily job slots. The same 19:00 closing deadline applies. Admission and pickup check the travel budget at the selected game speed; start this walking chain in the morning at 1×. Deadlines remain on the common game clock when speed changes.
- Completing an ordinary Ersatzteil-Express courier tour stocks a workshop part (capacity two). A later ordinary tube job consumes one, with no extra currency award. Existing courier contracts, progression and wages are retained.
- Larger numerical work progress, success/rework feedback, separate trip receipt and work receipts with total game minutes versus operation time. Save validation covers the new queue, cargo, feedback and receipts; malformed receipt text and duplicate customer assignments are rejected.
- Existing daily job limits, training prices and quality bonuses remain unchanged. All movement uses real seconds; operation timers use the selected 1×/4×/10× common clock, and menus freeze both.

Browser visual QA limitation: this buildless static Site has no compatible managed preview session in the current environment. Validation is automated logic, geometry, hand contact, clock and save coverage; a live visual playthrough was not performed here.

0.7.1 verification: all 220 existing regression checks passed in the full suite, including the 30-minute coupled pedestrian/traffic simulation. The final 11 workshop-extension checks passed, including every card variant, legacy interrupted work, atomic trip payment, actual courier stock delivery, customer reception priority at accelerated time, grounded feet and hand contact. All 65 local modules passed syntax, relative-import and uniform cache-version checks.


## 0.7.2 — Arbeitsbuch & Ablauf

Open **Tab → Arbeitsbuch**, or use the button on the assignments screen. The journal combines current workshop tasks, courier/cleaning/warehouse progress, deadlines, safe map navigation and an owned café shift. Workshop acceptance hours (08:00–17:00), closing at 19:00, remaining daily slots and café availability are displayed alongside actual work state. No remote completion or cash-collection controls are added.

- A new save-compatible `workLog` stores the last 40 itemized receipts and seven workday totals. It records workshop wages, parts-trip pay, courier wages and cleaning/warehouse pay once at their existing settlement points. Cancellation and expiry generate explicit zero-pay entries. Daily totals survive trimming older receipts from the same day. The module records transactions; it never grants currency, XP or reputation. Old saves start with an empty journal and keep existing last-receipt fields.
- Receipts show actual base pay, bonuses, deductions and recorded duration. Today and history filters use the established readable gold selection state; details expand natively. The journal labels wages as gross paid assignment income, excludes unclosed café cash and links users to the existing business/finance systems for costs and company results. It does not fabricate historical income.
- Newly started workshop operations first approach their physical work anchor at 1.65 metres per real second. A direct collision-clear segment is preferred; furniture-blocked approaches use the existing interaction point. Work begins after arrival while the common game clock continues throughout. Movement stays independent of 1×/4×/10×, carried parts remain in the hands, and walking pauses with menus. Standing on the work marker begins immediately. Saved 0.7.1 operations without an approach duration retain their existing timing.
- Work cards estimate remaining operation time plus indoor walking and handling time at the selected speed, and flag estimates that exceed time until closing. The UI explicitly excludes travel to the workshop, parts trips and additional rework. Parts trips warn when the player changes game speed after pickup; the agreed game-clock deadline remains unchanged.

Validation covers actual settlement integration, duplicate payment prevention, cancellation/expiry, bounded receipt history and persistent daily totals, old-save migration, read-only task navigation, physical approach speed, arrival-before-work, pause/resume and legacy active-operation timing. The static project has no compatible managed browser preview here, so no visual browser playthrough was performed.

0.7.2 verification: all 240 automated tests passed, including the long coupled pedestrian/traffic simulation. The nine workday tests passed again after refining the base/experience-bonus breakdown. Syntax, relative imports and uniform cache tags passed for all 67 local modules.


### 0.7.2 stability maintenance

New games start at 08:00 while existing saves resume at their saved time. Runtime assets are committed directly, save/import validation is stricter for discrete fields, businesses and dropped items, world-drop rendering no longer serializes the full drop list every frame, repeated owned vehicle models are bounded by a four-model cache, HUD DOM writes/minimap redraws are cached, and the workshop ambient day now follows the documented 08:00–19:00 window. GitHub Actions validates tests, JavaScript syntax, relative imports and critical static-server files on every push to this branch.

### Tree asset

All full-size city and horizon trees use the optimized `dist/assets/lindenstadt-tree.glb` model. The original embedded texture was reduced to 512×512 for browser performance while preserving the uploaded mesh geometry. Instances share one geometry/material and receive deterministic scale and rotation variation.

### Tree pit asset

Urban trees use the optimized `dist/assets/lindenstadt-tree-pit.glb` stone-bordered garden asset instead of the former procedurally generated metal grate / soil blocks. The uploaded model was reduced from roughly 153k to roughly 30.6k triangles and its three embedded textures were resized to 1024×1024. One shared geometry/material is rendered through instancing at every normal city tree; distant horizon trees intentionally remain without tree pits.
