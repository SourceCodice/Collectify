# Architettura

Collectify e' organizzato in due aree principali:

```text
src/
  backend/
    Collectify.Api/
  frontend/
    collectify-desktop/
```

## Backend

Il backend e' una Web API .NET 8 basata su Minimal API.

Il primo modulo verticale e' `Collections`:

```text
Modules/Collections/
  Collection.cs
  CollectionItem.cs
  CollectionDtos.cs
  CollectionEndpoints.cs
  CollectionMappings.cs
  ICollectionRepository.cs
  InMemoryCollectionRepository.cs
```

La dipendenza principale per l'accesso ai dati e' `ICollectionRepository`. Questo permette di sostituire il repository in memoria con un repository JSON, SQLite o altro storage senza cambiare gli endpoint.

## Frontend

Il frontend usa Electron per la shell desktop e React + TypeScript per il renderer.

```text
electron/
  main.ts
  preload.ts
src/
  app/
  features/
  shared/
  styles/
```

La UI comunica con il backend tramite `src/shared/api/collectifyClient.ts`.

## Avvio in sviluppo

In `Development`, il backend registra `FrontendLauncher`, che avvia il frontend Electron quando l'API e' pronta.

La modalita' e' configurabile in `appsettings.Development.json`:

```json
{
  "Development": {
    "FrontendLaunchMode": "Electron"
  }
}
```

## Evoluzione consigliata

1. Aggiungere persistenza JSON locale.
2. Introdurre dettaglio collezione e gestione oggetti.
3. Aggiungere test backend per endpoint e repository.
4. Preparare packaging desktop con Electron Builder.
