# Sviluppo locale

## Prerequisiti

- .NET SDK con supporto `net8.0`.
- Node.js e npm.
- Windows PowerShell.

## Installazione

Dalla cartella frontend:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd install
```

## Avvio

Dalla root del progetto:

```powershell
.\scripts\dev.ps1
```

Lo script avvia il backend. In ambiente `Development`, il backend avvia automaticamente Electron.

## URL locali

- API: `http://localhost:5088`
- Health check: `http://localhost:5088/health`
- Renderer Vite: `http://127.0.0.1:5173`

## Comandi utili

Backend:

```powershell
dotnet build .\Collectify.sln
dotnet run --project .\src\backend\Collectify.Api\Collectify.Api.csproj --launch-profile Collectify.Api
```

Frontend:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd run dev
npm.cmd run build
npm.cmd run typecheck
```

## Dati di sviluppo

I dati di sviluppo vengono salvati in:

```text
data/dev/collectify-data.json
data/dev/settings.json
data/dev/assets/images/
data/dev/backups/
```

Il file viene creato automaticamente al primo avvio. Se il JSON risulta corrotto, viene preservato con suffisso `.corrupt-*` e sostituito da un nuovo documento valido.

Per il modello dati completo vedi [DATA_MODEL.md](DATA_MODEL.md).

## Servizi esterni

Le chiavi API per TMDb, RAWG e Discogs vanno configurate in `src/backend/Collectify.Api/appsettings.Local.json`, partendo da `appsettings.Local.example.json`. Il file locale non deve essere versionato.

Gli endpoint disponibili sono documentati in [EXTERNAL_METADATA.md](EXTERNAL_METADATA.md).

## Ricerca locale

La ricerca lavora sui dati caricati dal file JSON locale e non usa database o servizi esterni.

Esempio:

```text
GET /api/search/items?query=blade&sortBy=name&sortDirection=asc
```

Sono disponibili filtri per categoria, tag, stato, valutazione numerica ricavata dagli attributi e intervallo data.

## Impostazioni locali

Le impostazioni vengono salvate in `data/dev/settings.json` durante lo sviluppo. L'endpoint locale e':

```text
GET /api/settings
PUT /api/settings
```

Da qui si possono aggiornare cartella dati, tema, backup automatico e lingua futura.

## Backup, export e import

Gli endpoint locali per la portabilita' dati sono:

```text
POST /api/data/backup
GET /api/data/export
POST /api/data/import
```

`/api/data/export` restituisce un file JSON unico con formato `collectify-export` e `formatVersion`. `/api/data/import` accetta `multipart/form-data` con campo `file` e aggiunge i dati importati senza sovrascrivere quelli esistenti.
