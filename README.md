# Collectify

Collectify e' un'app desktop per gestire collezioni personali di qualsiasi tipo: film, videogiochi, auto, piante, mobili, libri e categorie personalizzate.

Il progetto usa un backend .NET 8 Web API e un frontend Electron + React + TypeScript. In sviluppo non richiede un database: i dati sono gestiti da un repository in memoria, pensato per prototipare rapidamente dominio, API e interfaccia.

## Funzionalita iniziali

- Dashboard desktop Electron.
- Lista collezioni con dati seed.
- Creazione di nuove collezioni.
- API REST per collezioni e oggetti.
- Avvio locale con backend come punto d'ingresso.
- Configurazione pronta per GitHub, CI e aggiornamenti dipendenze.

## Struttura

```text
Collectify/
  src/
    backend/
      Collectify.Api/              # .NET 8 Web API
        Modules/Collections/       # Primo modulo verticale
    frontend/
      collectify-desktop/          # Electron + React + TypeScript
        electron/                  # Main process e preload
        src/app/                   # Shell applicativa
        src/features/collections/  # UI e tipi del modulo collezioni
        src/shared/api/            # Client HTTP verso il backend
  scripts/
    dev.ps1                        # Avvio locale guidato
```

## Avvio locale

Installa le dipendenze frontend una sola volta:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd install
```

Poi avvia Collectify dalla root:

```powershell
.\scripts\dev.ps1
```

Oppure avvia direttamente il backend:

```powershell
dotnet run --project .\src\backend\Collectify.Api\Collectify.Api.csproj --launch-profile Collectify.Api
```

In ambiente `Development` il backend prova ad avviare automaticamente anche il frontend Electron. Se il renderer Vite non e' gia' attivo, esegue `npm.cmd run dev`; se Vite e' gia' attivo, apre solo la finestra Electron.

Comandi frontend manuali:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd run dev
npm.cmd run build
```

Endpoint utili:

- API: `http://localhost:5088`
- Health check: `http://localhost:5088/health`
- Electron renderer: `http://127.0.0.1:5173`

## Stato attuale

- Backend .NET 8 con Minimal API.
- Modulo `Collections` con CRUD di base per collezioni e oggetti.
- Repository in memoria con dati seed per lo sviluppo.
- Frontend Electron + React + TypeScript con dashboard, lista collezioni e creazione collezione.
- CORS configurato per il renderer Vite locale.

## Verifiche

Backend:

```powershell
dotnet build .\Collectify.sln
```

Frontend:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd run build
```

## Sviluppo senza database

Per questa fase ti consiglio questo ordine:

1. `InMemoryCollectionRepository`, gia' incluso: velocissimo per prototipare dominio, API e UI. Ogni riavvio resetta i dati.
2. File JSON locale: stessa interfaccia repository, ma persistenza su `data/dev/collections.json` o su `%APPDATA%/Collectify`. E' leggibile, versionabile in esempi e perfetto prima di introdurre migrazioni.
3. IndexedDB/localStorage nel renderer: utile per esperimenti solo frontend, ma meno adatto se il backend deve restare la fonte dei dati.
4. SQLite quando il modello dati si stabilizza: e' un database embedded, non richiede server e si integra bene con app desktop.
5. LiteDB come alternativa documentale embedded per .NET: comoda per oggetti flessibili, ma aggiunge una dipendenza NuGet.

La scelta piu' naturale per il prossimo passo e' aggiungere un `JsonCollectionRepository` dietro la stessa `ICollectionRepository`, selezionabile da configurazione.

## Documentazione

- [Sviluppo locale](docs/DEVELOPMENT.md)
- [Architettura](docs/ARCHITECTURE.md)
- [Contribuire](CONTRIBUTING.md)
- [Sicurezza](SECURITY.md)

## Licenza

Collectify e' distribuito con licenza MIT. Vedi [LICENSE](LICENSE).
