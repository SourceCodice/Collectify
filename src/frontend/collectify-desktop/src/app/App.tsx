import { FormEvent, useEffect, useMemo, useState } from "react";
import { collectionTypeLabels, collectionTypes } from "../features/collections/CollectionTypeCatalog";
import type {
  CollectionDetail,
  CollectionItem,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../features/collections/types";
import type { LocalSearchResponse, LocalSearchResult } from "../features/search/types";
import type { AppSettings, UpdateAppSettingsPayload } from "../features/settings/types";
import { collectifyClient } from "../shared/api/collectifyClient";
import "./App.css";

type LoadState = "idle" | "loading" | "ready" | "error";
type SearchState = "idle" | "loading" | "ready" | "error";
type SettingsState = "idle" | "loading" | "ready" | "error";
type DataTransferState = "idle" | "running" | "success" | "error";

type AttributeDraft = {
  key: string;
  value: string;
};

type ImagePreview = {
  name: string;
  url: string;
};

type SearchFilters = {
  query: string;
  categoryId: string;
  tagId: string;
  condition: string;
  minRating: string;
  dateFrom: string;
  dateTo: string;
  dateField: "createdAt" | "updatedAt" | "acquiredAt";
  sortBy: "name" | "createdAt" | "updatedAt" | "value";
  sortDirection: "asc" | "desc";
};

type VisibleItem = {
  collectionId: string;
  collectionName?: string;
  categoryName?: string | null;
  item: CollectionItem;
  tags: LocalSearchResult["tags"];
  rating?: number | null;
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

const emptySearchFilters: SearchFilters = {
  query: "",
  categoryId: "",
  tagId: "",
  condition: "",
  minRating: "",
  dateFrom: "",
  dateTo: "",
  dateField: "updatedAt",
  sortBy: "updatedAt",
  sortDirection: "desc"
};

const emptySearchResponse: LocalSearchResponse = {
  totalCount: 0,
  items: [],
  facets: {
    categories: [],
    tags: [],
    conditions: [],
    ratingRange: null,
    valueRange: null
  }
};

const emptySettingsForm: UpdateAppSettingsPayload = {
  dataRootPath: "",
  theme: "System",
  automaticBackupEnabled: true,
  language: "it-IT"
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
  const [searchFilters, setSearchFilters] = useState<SearchFilters>(emptySearchFilters);
  const [searchResponse, setSearchResponse] = useState<LocalSearchResponse>(emptySearchResponse);
  const [searchState, setSearchState] = useState<SearchState>("idle");
  const [searchVersion, setSearchVersion] = useState(0);
  const [settingsModalOpen, setSettingsModalOpen] = useState(false);
  const [settingsState, setSettingsState] = useState<SettingsState>("idle");
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [settingsForm, setSettingsForm] = useState<UpdateAppSettingsPayload>(emptySettingsForm);
  const [dataTransferState, setDataTransferState] = useState<DataTransferState>("idle");
  const [dataTransferMessage, setDataTransferMessage] = useState("Backup, export e import lavorano solo sui file JSON locali.");

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
    void loadSettings();
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(async () => {
      setSearchState("loading");

      try {
        const data = await collectifyClient.searchItems({
          query: searchFilters.query.trim() || undefined,
          categoryId: searchFilters.categoryId || undefined,
          tagId: searchFilters.tagId || undefined,
          condition: searchFilters.condition || undefined,
          minRating: searchFilters.minRating || undefined,
          dateFrom: searchFilters.dateFrom || undefined,
          dateTo: searchFilters.dateTo || undefined,
          dateField: searchFilters.dateField,
          sortBy: searchFilters.sortBy,
          sortDirection: searchFilters.sortDirection
        });

        setSearchResponse(data);
        setSearchState("ready");
      } catch {
        setSearchState("error");
      }
    }, 220);

    return () => window.clearTimeout(timeoutId);
  }, [searchFilters, searchVersion]);

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

  const searchIsActive = useMemo(() => {
    return (
      Boolean(searchFilters.query.trim()) ||
      Boolean(searchFilters.categoryId) ||
      Boolean(searchFilters.tagId) ||
      Boolean(searchFilters.condition) ||
      Boolean(searchFilters.minRating) ||
      Boolean(searchFilters.dateFrom) ||
      Boolean(searchFilters.dateTo) ||
      searchFilters.sortBy !== emptySearchFilters.sortBy ||
      searchFilters.sortDirection !== emptySearchFilters.sortDirection
    );
  }, [searchFilters]);

  const visibleItems = useMemo<VisibleItem[]>(() => {
    if (searchIsActive) {
      return searchResponse.items.map((result) => ({
        collectionId: result.collectionId,
        collectionName: result.collectionName,
        categoryName: result.categoryName,
        item: result.item,
        tags: result.tags,
        rating: result.rating
      }));
    }

    return selectedCollection?.items.map((item) => ({
      collectionId: selectedCollection.id,
      item,
      tags: [],
      rating: null
    })) ?? [];
  }, [searchIsActive, searchResponse.items, selectedCollection]);

  function updateSearchFilter<Key extends keyof SearchFilters>(key: Key, value: SearchFilters[Key]) {
    setSearchFilters((current) => ({ ...current, [key]: value }));
  }

  function refreshSearchResults() {
    setSearchVersion((current) => current + 1);
  }

  async function loadSettings() {
    setSettingsState("loading");

    try {
      const data = await collectifyClient.getSettings();
      setSettings(data);
      setSettingsForm({
        dataRootPath: data.dataRootPath,
        theme: data.theme,
        automaticBackupEnabled: data.automaticBackupEnabled,
        language: data.language
      });
      setSettingsState("ready");
    } catch {
      setSettingsState("error");
    }
  }

  async function handleSaveSettings(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      const updated = await collectifyClient.updateSettings({
        dataRootPath: settingsForm.dataRootPath,
        theme: settingsForm.theme,
        automaticBackupEnabled: settingsForm.automaticBackupEnabled,
        language: settingsForm.language
      });

      setSettings(updated);
      setSettingsForm({
        dataRootPath: updated.dataRootPath,
        theme: updated.theme,
        automaticBackupEnabled: updated.automaticBackupEnabled,
        language: updated.language
      });
      setSettingsModalOpen(false);
      await loadCollections(selectedCollection?.id);
      refreshSearchResults();
      setMessage("Impostazioni salvate.");
    } catch {
      setMessage("Salvataggio impostazioni non riuscito.");
    }
  }

  async function handleCreateBackup() {
    setDataTransferState("running");
    setDataTransferMessage("Creazione backup in corso...");

    try {
      const result = await collectifyClient.createBackup();
      setDataTransferState("success");
      setDataTransferMessage(
        result.files.length > 0
          ? `Backup creato con ${result.files.length} file in ${result.backupDirectoryPath}.`
          : result.messages.join(" ") || "Backup completato, ma non sono stati copiati file."
      );
      setMessage("Backup locale creato.");
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Backup non riuscito.";
      setDataTransferState("error");
      setDataTransferMessage(errorMessage);
      setMessage(errorMessage);
    }
  }

  async function handleExportData() {
    setDataTransferState("running");
    setDataTransferMessage("Preparazione export JSON...");

    try {
      const fileName = await collectifyClient.exportData();
      setDataTransferState("success");
      setDataTransferMessage(`Export creato: ${fileName}.`);
      setMessage("Export JSON pronto.");
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Export non riuscito.";
      setDataTransferState("error");
      setDataTransferMessage(errorMessage);
      setMessage(errorMessage);
    }
  }

  async function handleImportData(files: FileList | null) {
    const file = files?.[0];
    if (!file) {
      return;
    }

    setDataTransferState("running");
    setDataTransferMessage("Validazione e import del file JSON...");

    try {
      const result = await collectifyClient.importData(file);
      await loadCollections(selectedCollection?.id);
      refreshSearchResults();
      setDataTransferState("success");
      setDataTransferMessage(
        `Import completato: ${result.importedCollections} collezioni e ${result.importedItems} elementi aggiunti. ${result.messages.join(" ")}`
      );
      setMessage("Import JSON completato.");
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Import non riuscito.";
      setDataTransferState("error");
      setDataTransferMessage(errorMessage);
      setMessage(errorMessage);
    }
  }

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
      refreshSearchResults();
      setMessage("Elemento aggiunto.");
    } catch {
      setMessage("Creazione elemento non riuscita.");
    }
  }

  async function handleUploadImages(collectionId: string, itemId: string, files: FileList | null) {
    if (!files?.length) {
      return;
    }

    try {
      for (const file of Array.from(files)) {
        await collectifyClient.uploadItemImage(collectionId, itemId, file);
      }

      await loadCollections(collectionId);
      refreshSearchResults();
      setMessage("Immagine aggiunta.");
    } catch {
      setMessage("Upload immagine non riuscito.");
    }
  }

  async function handleReplaceImage(collectionId: string, itemId: string, imageId: string, files: FileList | null) {
    if (!files?.[0]) {
      return;
    }

    try {
      await collectifyClient.replaceItemImage(collectionId, itemId, imageId, files[0]);
      await loadCollections(collectionId);
      refreshSearchResults();
      setMessage("Immagine sostituita.");
    } catch {
      setMessage("Sostituzione immagine non riuscita.");
    }
  }

  async function handleDeleteImage(collectionId: string, itemId: string, imageId: string) {
    try {
      await collectifyClient.deleteItemImage(collectionId, itemId, imageId);
      await loadCollections(collectionId);
      refreshSearchResults();
      setMessage("Immagine eliminata.");
    } catch {
      setMessage("Eliminazione immagine non riuscita.");
    }
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <img className="brand-mark" src="/collectify-logo.png" alt="" aria-hidden="true" />
          <div>
            <strong>Collectify</strong>
            <span>{collections.length} collezioni</span>
          </div>
        </div>

        <button className="primary-action" type="button" onClick={() => setCollectionModalOpen(true)}>
          <span aria-hidden="true">+</span>
          Nuova collezione
        </button>

        <button className="settings-action" type="button" onClick={() => setSettingsModalOpen(true)}>
          Impostazioni
        </button>

        <div className="data-path-card">
          <span>Cartella dati</span>
          <strong>{settings?.dataRootPath ?? (settingsState === "loading" ? "Caricamento..." : "Non disponibile")}</strong>
        </div>

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

        <section className="search-panel" aria-label="Ricerca locale">
          <div className="search-panel__bar">
            <input
              value={searchFilters.query}
              onChange={(event) => updateSearchFilter("query", event.target.value)}
              placeholder="Cerca per nome, descrizione, tag o attributi"
            />
            <button className="ghost-action" type="button" onClick={() => setSearchFilters(emptySearchFilters)}>
              Azzera
            </button>
          </div>
          <div className="search-panel__filters">
            <select value={searchFilters.categoryId} onChange={(event) => updateSearchFilter("categoryId", event.target.value)}>
              <option value="">Tutte le categorie</option>
              {searchResponse.facets.categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.label} ({category.count})
                </option>
              ))}
            </select>
            <select value={searchFilters.tagId} onChange={(event) => updateSearchFilter("tagId", event.target.value)}>
              <option value="">Tutti i tag</option>
              {searchResponse.facets.tags.map((tag) => (
                <option key={tag.id} value={tag.id}>
                  {tag.label} ({tag.count})
                </option>
              ))}
            </select>
            <select value={searchFilters.condition} onChange={(event) => updateSearchFilter("condition", event.target.value)}>
              <option value="">Tutti gli stati</option>
              {searchResponse.facets.conditions.map((condition) => (
                <option key={condition.id} value={condition.label}>
                  {condition.label} ({condition.count})
                </option>
              ))}
            </select>
            <input
              min={searchResponse.facets.ratingRange?.min ?? 0}
              max={searchResponse.facets.ratingRange?.max ?? 10}
              step="0.1"
              type="number"
              value={searchFilters.minRating}
              onChange={(event) => updateSearchFilter("minRating", event.target.value)}
              placeholder="Valutazione min."
            />
            <select value={searchFilters.dateField} onChange={(event) => updateSearchFilter("dateField", event.target.value as SearchFilters["dateField"])}>
              <option value="updatedAt">Data aggiornamento</option>
              <option value="createdAt">Data creazione</option>
              <option value="acquiredAt">Data acquisizione</option>
            </select>
            <input type="date" value={searchFilters.dateFrom} onChange={(event) => updateSearchFilter("dateFrom", event.target.value)} />
            <input type="date" value={searchFilters.dateTo} onChange={(event) => updateSearchFilter("dateTo", event.target.value)} />
            <select value={searchFilters.sortBy} onChange={(event) => updateSearchFilter("sortBy", event.target.value as SearchFilters["sortBy"])}>
              <option value="updatedAt">Ordina per aggiornamento</option>
              <option value="createdAt">Ordina per creazione</option>
              <option value="name">Ordina per nome</option>
              <option value="value">Ordina per valore</option>
            </select>
            <select value={searchFilters.sortDirection} onChange={(event) => updateSearchFilter("sortDirection", event.target.value as SearchFilters["sortDirection"])}>
              <option value="desc">Decrescente</option>
              <option value="asc">Crescente</option>
            </select>
          </div>
        </section>

        <section className="items-section">
          <div className="section-heading">
            <div>
              <p className="eyebrow">{searchIsActive ? "Ricerca locale" : "Elementi"}</p>
              <h2>{searchIsActive ? `${searchResponse.totalCount} risultati` : selectedCollection ? selectedCollection.name : "Nessuna collezione selezionata"}</h2>
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
          {searchState === "error" && <div className="empty-state">La ricerca locale non e' disponibile finche' il backend non risponde.</div>}
          {searchState === "loading" && searchIsActive && <div className="empty-state">Aggiornamento risultati...</div>}

          {loadState !== "error" && !searchIsActive && !selectedCollection && (
            <div className="empty-state">Crea una collezione dalla sidebar per iniziare il tuo archivio.</div>
          )}

          {!searchIsActive && selectedCollection && selectedCollection.items.length === 0 && (
            <div className="empty-state">Questa collezione e' ancora vuota. Aggiungi il primo elemento.</div>
          )}

          {searchIsActive && searchState !== "loading" && visibleItems.length === 0 && (
            <div className="empty-state">Nessun elemento corrisponde ai filtri attuali.</div>
          )}

          <div className="items-list">
            {visibleItems.map(({ collectionId, collectionName, categoryName, item, tags, rating }) => (
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
                  {searchIsActive && (
                    <div className="result-meta">
                      <span>{collectionName}</span>
                      {categoryName && <span>{categoryName}</span>}
                      {typeof rating === "number" && <span>Valutazione {rating}</span>}
                    </div>
                  )}
                  {(item.description || item.notes) && <p>{item.description || item.notes}</p>}
                  {tags.length > 0 && (
                    <div className="tag-list">
                      {tags.map((tag) => (
                        <span key={tag.id}>{tag.label}</span>
                      ))}
                    </div>
                  )}
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
                                onChange={(event) => void handleReplaceImage(collectionId, item.id, image.id, event.currentTarget.files)}
                              />
                            </label>
                            <button type="button" onClick={() => void handleDeleteImage(collectionId, item.id, image.id)}>
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
                      onChange={(event) => void handleUploadImages(collectionId, item.id, event.currentTarget.files)}
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

      {settingsModalOpen && (
        <div className="modal-backdrop" role="presentation">
          <form className="modal modal--wide" onSubmit={handleSaveSettings}>
            <div className="modal__header">
              <h2>Impostazioni</h2>
              <button type="button" onClick={() => setSettingsModalOpen(false)} aria-label="Chiudi">
                x
              </button>
            </div>
            <div className="settings-summary">
              <div>
                <span>File impostazioni</span>
                <strong>{settings?.settingsFilePath ?? "Non disponibile"}</strong>
              </div>
              <div>
                <span>File dati</span>
                <strong>{settings?.dataFilePath ?? "Non disponibile"}</strong>
              </div>
            </div>
            <label>
              Cartella dati
              <input
                value={settingsForm.dataRootPath ?? ""}
                onChange={(event) => setSettingsForm((current) => ({ ...current, dataRootPath: event.target.value }))}
                placeholder="Percorso locale della cartella dati"
              />
            </label>
            <div className="field-grid">
              <label>
                Tema
                <select
                  value={settingsForm.theme}
                  onChange={(event) => setSettingsForm((current) => ({ ...current, theme: event.target.value }))}
                >
                  <option value="System">Sistema</option>
                  <option value="Dark">Scuro</option>
                  <option value="Light">Chiaro</option>
                </select>
              </label>
              <label>
                Lingua
                <select
                  value={settingsForm.language}
                  onChange={(event) => setSettingsForm((current) => ({ ...current, language: event.target.value }))}
                >
                  <option value="it-IT">Italiano</option>
                  <option value="en-US">English</option>
                </select>
              </label>
            </div>
            <label className="switch-field">
              <input
                checked={Boolean(settingsForm.automaticBackupEnabled)}
                type="checkbox"
                onChange={(event) =>
                  setSettingsForm((current) => ({ ...current, automaticBackupEnabled: event.target.checked }))
                }
              />
              <span>Backup automatico locale</span>
            </label>
            <div className="settings-summary">
              <div>
                <span>Immagini</span>
                <strong>{settings?.imagesPath ?? "Non disponibile"}</strong>
              </div>
              <div>
                <span>Backup</span>
                <strong>{settings?.backupsPath ?? "Non disponibile"}</strong>
              </div>
            </div>
            <section className="data-transfer-panel" aria-label="Backup export import">
              <div className="data-transfer-panel__header">
                <div>
                  <span>Portabilita' dati</span>
                  <strong>Backup, export e import JSON</strong>
                </div>
              </div>
              <div className="data-transfer-actions">
                <button
                  className="secondary-action"
                  disabled={dataTransferState === "running"}
                  type="button"
                  onClick={() => void handleCreateBackup()}
                >
                  Crea backup
                </button>
                <button
                  className="secondary-action"
                  disabled={dataTransferState === "running"}
                  type="button"
                  onClick={() => void handleExportData()}
                >
                  Esporta JSON
                </button>
                <label className={dataTransferState === "running" ? "import-action is-disabled" : "import-action"}>
                  Importa JSON
                  <input
                    accept="application/json,.json"
                    disabled={dataTransferState === "running"}
                    type="file"
                    onChange={(event) => {
                      void handleImportData(event.currentTarget.files);
                      event.currentTarget.value = "";
                    }}
                  />
                </label>
              </div>
              <p className={`data-transfer-message data-transfer-message--${dataTransferState}`}>{dataTransferMessage}</p>
            </section>
            <button className="primary-action" type="submit">
              Salva impostazioni
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
