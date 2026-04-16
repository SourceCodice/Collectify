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

Attualmente i dati sono in memoria e vengono rigenerati a ogni avvio del backend. Il prossimo passo consigliato e' introdurre un repository JSON locale dietro la stessa interfaccia `ICollectionRepository`.
