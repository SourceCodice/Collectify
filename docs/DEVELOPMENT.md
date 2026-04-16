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
data/dev/assets/images/
```

Il file viene creato automaticamente al primo avvio. Se il JSON risulta corrotto, viene preservato con suffisso `.corrupt-*` e sostituito da un nuovo documento valido.

Per il modello dati completo vedi [DATA_MODEL.md](DATA_MODEL.md).
