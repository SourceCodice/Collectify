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

La dipendenza principale per l'accesso ai dati e' `ICollectionRepository`. L'implementazione attuale usa repository JSON file-based, senza database.

La persistenza vive nel namespace `Collectify.Api.Persistence`:

```text
Persistence/
  CollectifyDataDocument.cs
  CollectifySeedData.cs
  ICollectifyDataStore.cs
  JsonCollectifyDataStore.cs
  JsonCollectionRepository.cs
  JsonUserProfileRepository.cs
  JsonCollectionCategoryRepository.cs
  JsonTagRepository.cs
  JsonAppSettingsRepository.cs
  LocalDataOptions.cs
```

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

1. Introdurre dettaglio collezione e gestione oggetti.
2. Aggiungere test backend per endpoint e repository.
3. Preparare packaging desktop con Electron Builder.
4. Aggiungere migrazioni JSON se lo schema locale evolve.
