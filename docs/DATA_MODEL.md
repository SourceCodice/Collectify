# Modello Dati

Collectify usa identificatori `Guid` stabili per tutte le entita' principali. Gli identificatori vengono generati alla creazione e non cambiano durante gli aggiornamenti.

## Entita'

- `UserProfile`: preferenze e dati base dell'utente locale.
- `Collection`: contenitore principale; include molti `Item`.
- `CollectionCategory`: categoria riutilizzabile per raggruppare collezioni.
- `Item`: oggetto collezionato.
- `ItemImage`: immagine locale associata a un item.
- `Tag`: etichetta riutilizzabile tra item diversi.
- `ExternalReference`: riferimento verso provider esterni.
- `ItemAttribute`: attributo dinamico per adattare un item a tipi di collezione diversi.
- `AppSettings`: impostazioni applicative locali.

## Relazioni

- Una `Collection` contiene molti `Item`.
- Un `Item` contiene una lista di `ItemAttribute`.
- Un `Item` puo' avere zero o piu' `Tag`, referenziati tramite `TagIds`.
- Un `Item` puo' avere zero o piu' `ExternalReference`.
- Un `Item` puo' avere zero o piu' `ItemImage`.
- Una `Collection` puo' avere una `CollectionCategory`.

## Audit

Tutte le entita' principali includono:

- `createdAt`
- `updatedAt`

`updatedAt` viene aggiornato dai repository quando un'entita' viene salvata o modificata.

## Persistenza JSON

I dati locali sono salvati in un unico documento JSON:

```text
collectify-data.json
```

La struttura del documento contiene:

```text
schemaVersion
userProfile
appSettings
categories
tags
collections
createdAt
updatedAt
```

Le immagini non vengono salvate dentro il JSON: il modello conserva percorso relativo e metadati, mentre i file immagine vivono nella cartella `assets/images/`.

Esempio:

```json
{
  "relativePath": "assets/images/20260416120000000-<guid>.png"
}
```

Il file viene servito localmente dal backend tramite:

```text
/api/assets/assets/images/<file>
```

## Scrittura Atomica

Il salvataggio avviene cosi':

1. serializzazione completa su file temporaneo nella stessa cartella;
2. flush del file temporaneo;
3. replace del file JSON finale;
4. rimozione del backup temporaneo.

Questo riduce il rischio di lasciare un file parziale se l'app viene chiusa durante la scrittura.

## Asset Locali

Gli upload immagine usano `multipart/form-data`. Il backend copia il file nella cartella locale dell'app e salva nel JSON solo il riferimento relativo.

Endpoint principali:

- `POST /api/collections/{collectionId}/items/{itemId}/images`
- `PUT /api/collections/{collectionId}/items/{itemId}/images/{imageId}`
- `DELETE /api/collections/{collectionId}/items/{itemId}/images/{imageId}`
- `GET /api/assets/{relativePath}`

I nomi file includono timestamp e `Guid`, cosi' non entrano in collisione con altri upload.

## File Mancante

Se il file JSON non esiste, viene creato un documento iniziale con dati seed minimi.

## JSON Corrotto

Se il JSON non e' leggibile, il file viene spostato con suffisso:

```text
.corrupt-yyyyMMddHHmmss
```

Poi viene creato un nuovo documento valido. Il file corrotto resta disponibile per recupero manuale.
