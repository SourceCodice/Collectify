# Contribuire

Grazie per l'interesse in Collectify.

## Setup

1. Installa .NET SDK con supporto per `net8.0`.
2. Installa Node.js e npm.
3. Installa le dipendenze frontend:

```powershell
cd .\src\frontend\collectify-desktop
npm.cmd install
```

4. Avvia l'app dalla root:

```powershell
.\scripts\dev.ps1
```

## Linee guida

- Mantieni le modifiche piccole e focalizzate.
- Segui la struttura modulare esistente in `src/backend/Collectify.Api/Modules`.
- Evita nuove dipendenze finche' il problema puo' essere risolto con lo stack gia' presente.
- Aggiorna README o documentazione quando cambi comandi, configurazione o comportamento visibile.
- Prima di aprire una pull request, esegui:

```powershell
dotnet build .\Collectify.sln
cd .\src\frontend\collectify-desktop
npm.cmd run build
```

## Branch e commit

Usa branch descrittivi, per esempio:

```text
feature/json-storage
fix/electron-startup
docs/readme-setup
```

Preferisci messaggi di commit brevi e concreti, come `Add JSON storage repository` o `Fix Electron startup on Windows`.
