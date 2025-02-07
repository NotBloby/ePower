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
│── backend/
│   ├── ePower.sln
│   ├── src/
│   │   ├── Program.cs
│   │   ├── USB.cs
│   │   ├── Battery.cs
│   │   ├── Server.cs
│
│── frontend/    
│   ├── index.html
│   ├── styles.css
│   ├── script.js
│
│── python/
│   ├── analyze.py
│   ├── requirements.txt
│
│── tests/
│   ├── test_backend.py
│   ├── test_frontend.js 
│
│── config.json
│── .gitignore
│── LICENSE
│── README.md
```

## Instalace a spuštění

### Požadavky
- .NET 6+
- Python 3.8+ (volitelně pro analytické funkce)
- Webový prohlížeč

### Instalace
1. Naklonujte repozitář:
   ```sh
   git clone https://github.com/NotBloby/ePower.git
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


