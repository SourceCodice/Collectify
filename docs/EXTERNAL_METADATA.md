# Integrazione esterna

Collectify interroga i servizi esterni solo dal backend .NET. Il frontend Electron usa esclusivamente gli endpoint locali dell'API Collectify, quindi le chiavi dei provider non vengono mai esposte nel renderer.

## Provider

- `tmdb`: film
- `rawg`: videogiochi
- `discogs`: album

Ogni provider implementa `IExternalMetadataProvider` con:

- `SearchAsync(query)`
- `GetDetailsAsync(externalId)`

I risultati vengono mappati in un modello interno comune composto da titolo, descrizione, data, immagine remota di riferimento, attributi dinamici e metadati.

## Configurazione locale

Copiare:

```text
src/backend/Collectify.Api/appsettings.Local.example.json
```

in:

```text
src/backend/Collectify.Api/appsettings.Local.json
```

e inserire le chiavi API locali. Il file `appsettings.Local.json` e' ignorato da Git.

## Endpoint di ricerca

```text
GET /api/external/movies/search?query=dune
GET /api/external/games/search?query=zelda
GET /api/external/albums/search?query=kind%20of%20blue
```

## Endpoint di dettaglio

```text
GET /api/external/movies/{tmdbId}
GET /api/external/games/{rawgId}
GET /api/external/albums/{discogsMasterId}
```

## Import nei dati locali

```http
POST /api/external/import
Content-Type: application/json

{
  "collectionId": "00000000-0000-0000-0000-000000000000",
  "provider": "tmdb",
  "externalId": "438631"
}
```

L'import crea un `Item` nella collezione scelta e lo salva nel file JSON locale. Le immagini remote restano metadati del riferimento esterno: non vengono scaricate automaticamente negli asset locali.

## Rate limit e retry

Ogni provider applica un limite base di richieste al secondo. Le risposte `429` e gli errori `5xx` vengono ritentati con backoff esponenziale configurabile.
