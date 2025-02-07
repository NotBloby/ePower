# ePower 🔋

## Popis projektu
**ePower** je aplikace pro monitorování stavu powerbanky připojené k počítači přes USB. Automaticky detekuje připojení powerbanky a zobrazí její aktuální stav včetně životnosti a nabití.

## Funkce
- Automatická detekce připojené powerbanky
- Zobrazení aktuálního nabití powerbanky
- Informace o zdraví (životnosti) baterie
- Uživatelsky přívětivé rozhraní postavené na HTML, CSS a JavaScriptu
- Možnost rozšíření o analytické funkce v Pythonu

## Technologie
- **Backend:** C# (ASP.NET Core)
- **Frontend:** HTML, CSS, JavaScript
- **Analytika:** Python (volitelné)

## Struktura projektu
```
ePower/
│── backend/              # Backendová část v C#
│   ├── PowerbankMonitor.sln   # Projektové řešení pro Visual Studio
│   ├── src/
│   │   ├── Program.cs    # Hlavní soubor aplikace
│   │   ├── UsbDetector.cs  # Detekce připojené powerbanky
│   │   ├── BatteryStatus.cs  # Získání informací o stavu baterie
│   │   ├── Server.cs  # API server pro komunikaci s frontendem
│
│── frontend/             # Frontendová část (HTML, CSS, JS)
│   ├── index.html        # Hlavní stránka aplikace
│   ├── styles.css        # CSS pro vzhled aplikace
│   ├── script.js         # JavaScript pro zpracování dat
│
│── python/               # Python pro analýzu dat (volitelné)
│   ├── analyze.py        # Skript pro pokročilé zpracování dat
│   ├── requirements.txt  # Seznam potřebných knihoven
│
│── docs/                 # Dokumentace k projektu
│   ├── README.md         # Popis projektu
│   ├── API_Documentation.md  # Popis API
│
│── tests/                # Testovací soubory
│   ├── test_backend.py   # Testy pro backend
│   ├── test_frontend.js  # Testy pro frontend
│
│── config.json           # Konfigurační soubor pro aplikaci
│── .gitignore            # Ignorované soubory pro Git
│── LICENSE               # Licence projektu
│── README.md             # Hlavní popis projektu
```

## Instalace a spuštění

### Požadavky
- .NET 6+
- Python 3.8+ (volitelně pro analytické funkce)
- Webový prohlížeč

### Instalace
1. Naklonujte repozitář:
   ```sh
   git clone https://github.com/tvuj-username/ePower.git
   cd ePower
   ```

2. Spusť backend:
   ```sh
   cd backend
   dotnet run
   ```

3. Otevři `index.html` ve webovém prohlížeči.

## Přispívání
Chceš se podílet na vývoji? Neváhej a vytvoř **pull request** nebo nahlas chybu v sekci Issues!

## Licence
Tento projekt je licencován pod GNU licencí.


