# Große Dateien wiederherstellen

Dieser Branch enthält den vollständigen Stand von **ZERO // RISE 0.7.2**.

Vier große, unveränderte Projektdateien liegen wegen der Übertragungsgrenze der GitHub-Schnittstelle verlustfrei unter `.zero-rise-parts/`. Nach dem Klonen einmal im Repository ausführen:

```bash
python3 tools/restore_large_files.py
```

Danach sind auch die drei Grafik-Assets und `dist/vendor/three.module.js` wieder an ihren ursprünglichen Pfaden vorhanden und das Spiel kann normal gestartet bzw. gebaut werden.
