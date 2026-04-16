import { FormEvent, useEffect, useMemo, useState } from "react";
import { collectionTypeLabels, collectionTypes } from "../features/collections/CollectionTypeCatalog";
import type {
  CollectionDetail,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../features/collections/types";
import { collectifyClient } from "../shared/api/collectifyClient";
import "./App.css";

type LoadState = "idle" | "loading" | "ready" | "error";

type AttributeDraft = {
  key: string;
  value: string;
};

type ImagePreview = {
  name: string;
  url: string;
};

const emptyCollectionForm: CreateCollectionPayload = {
  name: "",
  type: "Custom",
  description: "",
  categoryId: null
};

const emptyItemForm: CreateItemPayload = {
  title: "",
  description: "",
  notes: "",
  condition: "Non specificato",
  acquiredAt: null,
  attributes: [],
  externalReferences: []
};

export function App() {
  const [collections, setCollections] = useState<CollectionSummary[]>([]);
  const [selectedCollection, setSelectedCollection] = useState<CollectionDetail | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [message, setMessage] = useState("Pronto");
  const [collectionModalOpen, setCollectionModalOpen] = useState(false);
  const [itemModalOpen, setItemModalOpen] = useState(false);
  const [collectionForm, setCollectionForm] = useState<CreateCollectionPayload>(emptyCollectionForm);
  const [itemForm, setItemForm] = useState<CreateItemPayload>(emptyItemForm);
  const [attributeDrafts, setAttributeDrafts] = useState<AttributeDraft[]>([{ key: "", value: "" }]);
  const [itemImageFiles, setItemImageFiles] = useState<File[]>([]);
  const [itemImagePreviews, setItemImagePreviews] = useState<ImagePreview[]>([]);

  async function loadCollections(preferredCollectionId?: string) {
    setLoadState("loading");

    try {
      const data = await collectifyClient.listCollections();
      setCollections(data);

      const targetId = preferredCollectionId ?? selectedCollection?.id ?? data[0]?.id;
      if (targetId) {
        await openCollection(targetId, false);
      } else {
        setSelectedCollection(null);
      }

      setLoadState("ready");
      setMessage("Archivio sincronizzato");
    } catch {
      setLoadState("error");
      setMessage(`API non raggiungibile: ${collectifyClient.apiBaseUrl}`);
    }
  }

  async function openCollection(id: string, setBusy = true) {
    if (setBusy) {
      setLoadState("loading");
    }

    try {
      const detail = await collectifyClient.getCollection(id);
      setSelectedCollection(detail);
      setLoadState("ready");
      setMessage(`Collezione aperta: ${detail.name}`);
    } catch {
      setLoadState("error");
      setMessage("Non riesco ad aprire la collezione.");
    }
  }

  useEffect(() => {
    void loadCollections();
  }, []);

  useEffect(() => {
    const previews = itemImageFiles.map((file) => ({
      name: file.name,
      url: URL.createObjectURL(file)
    }));

    setItemImagePreviews(previews);

    return () => {
      previews.forEach((preview) => URL.revokeObjectURL(preview.url));
    };
  }, [itemImageFiles]);

  const totalItems = useMemo(
    () => collections.reduce((total, collection) => total + collection.itemCount, 0),
    [collections]
  );

  async function handleCreateCollection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!collectionForm.name.trim()) {
      setMessage("Inserisci un nome per la collezione.");
      return;
    }

    try {
      const created = await collectifyClient.createCollection(collectionForm);
      setCollectionForm(emptyCollectionForm);
      setCollectionModalOpen(false);
      await loadCollections(created.id);
      setMessage("Collezione creata.");
    } catch {
      setMessage("Creazione collezione non riuscita.");
    }
  }

  async function handleCreateItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedCollection) {
      return;
    }

    if (!itemForm.title?.trim()) {
      setMessage("Inserisci un titolo per l'elemento.");
      return;
    }

    const attributes = attributeDrafts
      .filter((attribute) => attribute.key.trim() || attribute.value.trim())
      .map((attribute) => ({
        key: attribute.key.trim(),
        label: attribute.key.trim(),
        value: attribute.value.trim(),
        valueType: "Text"
      }));

    try {
      const created = await collectifyClient.addItem(selectedCollection.id, {
        ...itemForm,
        acquiredAt: itemForm.acquiredAt ? new Date(`${itemForm.acquiredAt}T00:00:00`).toISOString() : null,
        attributes
      });

      for (const [index, file] of itemImageFiles.entries()) {
        await collectifyClient.uploadItemImage(selectedCollection.id, created.id, file, {
          isPrimary: index === 0
        });
      }

      setItemForm(emptyItemForm);
      setAttributeDrafts([{ key: "", value: "" }]);
      setItemImageFiles([]);
      setItemModalOpen(false);
      await loadCollections(selectedCollection.id);
      setMessage("Elemento aggiunto.");
    } catch {
      setMessage("Creazione elemento non riuscita.");
    }
  }

  async function handleUploadImages(itemId: string, files: FileList | null) {
    if (!selectedCollection || !files?.length) {
      return;
    }

    try {
      for (const file of Array.from(files)) {
        await collectifyClient.uploadItemImage(selectedCollection.id, itemId, file);
      }

      await loadCollections(selectedCollection.id);
      setMessage("Immagine aggiunta.");
    } catch {
      setMessage("Upload immagine non riuscito.");
    }
  }

  async function handleReplaceImage(itemId: string, imageId: string, files: FileList | null) {
    if (!selectedCollection || !files?.[0]) {
      return;
    }

    try {
      await collectifyClient.replaceItemImage(selectedCollection.id, itemId, imageId, files[0]);
      await loadCollections(selectedCollection.id);
      setMessage("Immagine sostituita.");
    } catch {
      setMessage("Sostituzione immagine non riuscita.");
    }
  }

  async function handleDeleteImage(itemId: string, imageId: string) {
    if (!selectedCollection) {
      return;
    }

    try {
      await collectifyClient.deleteItemImage(selectedCollection.id, itemId, imageId);
      await loadCollections(selectedCollection.id);
      setMessage("Immagine eliminata.");
    } catch {
      setMessage("Eliminazione immagine non riuscita.");
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <div className="brand-mark" aria-hidden="true">
            C
          </div>
          <div>
            <strong>Collectify</strong>
            <span>{collections.length} collezioni</span>
          </div>
        </div>

        <button className="primary-action" type="button" onClick={() => setCollectionModalOpen(true)}>
          <span aria-hidden="true">+</span>
          Nuova collezione
        </button>

        <nav className="collection-nav" aria-label="Collezioni">
          {collections.map((collection) => (
            <button
              className={collection.id === selectedCollection?.id ? "collection-nav__item is-active" : "collection-nav__item"}
              key={collection.id}
              type="button"
              onClick={() => void openCollection(collection.id)}
            >
              <span className="collection-nav__icon" aria-hidden="true">
                {collection.name.slice(0, 1).toUpperCase()}
              </span>
              <span>
                <strong>{collection.name}</strong>
                <small>{collection.itemCount} elementi</small>
              </span>
            </button>
          ))}
        </nav>
      </aside>

      <main className="main-panel">
        <header className="topbar">
          <div>
            <p className="eyebrow">Archivio personale</p>
            <h1>{selectedCollection?.name ?? "Le tue collezioni"}</h1>
          </div>
          <div className={`connection-pill connection-pill--${loadState}`}>
            <span aria-hidden="true" />
            {message}
          </div>
        </header>

        <section className="hero-panel">
          <div>
            <span className="type-pill">{selectedCollection ? collectionTypeLabels[selectedCollection.type] ?? selectedCollection.type : "Workspace"}</span>
            <h2>{selectedCollection?.description || "Seleziona una collezione o creane una nuova per iniziare."}</h2>
          </div>
          <div className="metrics">
            <div>
              <strong>{collections.length}</strong>
              <span>collezioni</span>
            </div>
            <div>
              <strong>{totalItems}</strong>
              <span>elementi totali</span>
            </div>
            <div>
              <strong>{selectedCollection?.items.length ?? 0}</strong>
              <span>in questa vista</span>
            </div>
          </div>
        </section>

        <section className="items-section">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Elementi</p>
              <h2>{selectedCollection ? selectedCollection.name : "Nessuna collezione selezionata"}</h2>
            </div>
            <button
              className="secondary-action"
              type="button"
              disabled={!selectedCollection}
              onClick={() => setItemModalOpen(true)}
            >
              <span aria-hidden="true">+</span>
              Nuovo elemento
            </button>
          </div>

          {loadState === "error" && <div className="empty-state">Avvia il backend per leggere le collezioni locali.</div>}

          {loadState !== "error" && !selectedCollection && (
            <div className="empty-state">Crea una collezione dalla sidebar per iniziare il tuo archivio.</div>
          )}

          {selectedCollection && selectedCollection.items.length === 0 && (
            <div className="empty-state">Questa collezione e' ancora vuota. Aggiungi il primo elemento.</div>
          )}

          <div className="items-list">
            {selectedCollection?.items.map((item) => (
              <article className="item-row" key={item.id}>
                <div className="item-row__avatar" aria-hidden="true">
                  {item.images[0] ? (
                    <img src={collectifyClient.resolveAssetUrl(item.images[0].url)} alt="" />
                  ) : (
                    item.title.slice(0, 1).toUpperCase()
                  )}
                </div>
                <div className="item-row__content">
                  <div className="item-row__title">
                    <h3>{item.title}</h3>
                    <span>{item.condition}</span>
                  </div>
                  {(item.description || item.notes) && <p>{item.description || item.notes}</p>}
                  {item.attributes.length > 0 && (
                    <div className="attribute-list">
                      {item.attributes.map((attribute) => (
                        <span key={attribute.id}>
                          {attribute.label}: {attribute.value}
                        </span>
                      ))}
                    </div>
                  )}
                  {item.images.length > 0 && (
                    <div className="image-gallery">
                      {item.images.map((image) => (
                        <div className="image-thumb" key={image.id}>
                          <img src={collectifyClient.resolveAssetUrl(image.url)} alt={image.caption ?? item.title} />
                          <div className="image-thumb__actions">
                            <label>
                              Sostituisci
                              <input
                                accept="image/gif,image/jpeg,image/png,image/webp"
                                type="file"
                                onChange={(event) => void handleReplaceImage(item.id, image.id, event.currentTarget.files)}
                              />
                            </label>
                            <button type="button" onClick={() => void handleDeleteImage(item.id, image.id)}>
                              Elimina
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                  <label className="inline-upload">
                    Aggiungi immagine
                    <input
                      accept="image/gif,image/jpeg,image/png,image/webp"
                      multiple
                      type="file"
                      onChange={(event) => void handleUploadImages(item.id, event.currentTarget.files)}
                    />
                  </label>
                </div>
              </article>
            ))}
          </div>
        </section>
      </main>

      {collectionModalOpen && (
        <div className="modal-backdrop" role="presentation">
          <form className="modal" onSubmit={handleCreateCollection}>
            <div className="modal__header">
              <h2>Nuova collezione</h2>
              <button type="button" onClick={() => setCollectionModalOpen(false)} aria-label="Chiudi">
                x
              </button>
            </div>
            <label>
              Nome
              <input
                value={collectionForm.name}
                onChange={(event) => setCollectionForm((current) => ({ ...current, name: event.target.value }))}
                placeholder="Es. Libreria personale"
              />
            </label>
            <label>
              Tipo
              <select
                value={collectionForm.type}
                onChange={(event) => setCollectionForm((current) => ({ ...current, type: event.target.value }))}
              >
                {collectionTypes.map((type) => (
                  <option key={type} value={type}>
                    {collectionTypeLabels[type]}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Descrizione
              <textarea
                value={collectionForm.description}
                onChange={(event) => setCollectionForm((current) => ({ ...current, description: event.target.value }))}
                placeholder="Cosa vuoi tenere sotto controllo?"
                rows={4}
              />
            </label>
            <button className="primary-action" type="submit">
              Crea collezione
            </button>
          </form>
        </div>
      )}

      {itemModalOpen && selectedCollection && (
        <div className="modal-backdrop" role="presentation">
          <form className="modal modal--wide" onSubmit={handleCreateItem}>
            <div className="modal__header">
              <h2>Nuovo elemento</h2>
              <button type="button" onClick={() => setItemModalOpen(false)} aria-label="Chiudi">
                x
              </button>
            </div>
            <label>
              Titolo
              <input
                value={itemForm.title}
                onChange={(event) => setItemForm((current) => ({ ...current, title: event.target.value }))}
                placeholder="Es. Dune"
              />
            </label>
            <div className="field-grid">
              <label>
                Stato
                <input
                  value={itemForm.condition}
                  onChange={(event) => setItemForm((current) => ({ ...current, condition: event.target.value }))}
                  placeholder="Ottimo"
                />
              </label>
              <label>
                Data acquisizione
                <input
                  type="date"
                  value={itemForm.acquiredAt ?? ""}
                  onChange={(event) => setItemForm((current) => ({ ...current, acquiredAt: event.target.value }))}
                />
              </label>
            </div>
            <label>
              Descrizione
              <textarea
                value={itemForm.description}
                onChange={(event) => setItemForm((current) => ({ ...current, description: event.target.value }))}
                placeholder="Dettagli principali dell'elemento."
                rows={3}
              />
            </label>
            <label>
              Note
              <textarea
                value={itemForm.notes}
                onChange={(event) => setItemForm((current) => ({ ...current, notes: event.target.value }))}
                placeholder="Promemoria, provenienza, difetti, desiderata."
                rows={3}
              />
            </label>
            <div className="attribute-editor">
              <div className="attribute-editor__header">
                <span>Attributi dinamici</span>
                <button type="button" onClick={() => setAttributeDrafts((current) => [...current, { key: "", value: "" }])}>
                  +
                </button>
              </div>
              {attributeDrafts.map((attribute, index) => (
                <div className="attribute-editor__row" key={index}>
                  <input
                    value={attribute.key}
                    onChange={(event) =>
                      setAttributeDrafts((current) =>
                        current.map((item, itemIndex) =>
                          itemIndex === index ? { ...item, key: event.target.value } : item
                        )
                      )
                    }
                    placeholder="Campo"
                  />
                  <input
                    value={attribute.value}
                    onChange={(event) =>
                      setAttributeDrafts((current) =>
                        current.map((item, itemIndex) =>
                          itemIndex === index ? { ...item, value: event.target.value } : item
                        )
                      )
                    }
                    placeholder="Valore"
                  />
                </div>
              ))}
            </div>
            <div className="image-picker">
              <div className="attribute-editor__header">
                <span>Immagini locali</span>
                <label>
                  Scegli
                  <input
                    accept="image/gif,image/jpeg,image/png,image/webp"
                    multiple
                    type="file"
                    onChange={(event) => setItemImageFiles(Array.from(event.currentTarget.files ?? []))}
                  />
                </label>
              </div>
              {itemImagePreviews.length === 0 && <p>Nessuna immagine selezionata.</p>}
              {itemImagePreviews.length > 0 && (
                <div className="image-preview-grid">
                  {itemImagePreviews.map((preview) => (
                    <figure key={preview.url}>
                      <img src={preview.url} alt={preview.name} />
                      <figcaption>{preview.name}</figcaption>
                    </figure>
                  ))}
                </div>
              )}
            </div>
            <button className="primary-action" type="submit">
              Aggiungi elemento
            </button>
          </form>
        </div>
      )}
    </div>
  );
}
