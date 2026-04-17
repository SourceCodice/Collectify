import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  collectionTypeLabels,
  collectionTypes,
  getDefaultItemType,
  itemTypeOptionsByCollectionType
} from "../features/collections/CollectionTypeCatalog";
import type {
  CollectionDetail,
  CollectionItem,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../features/collections/types";
import { DataTransferPanel } from "../features/dataTransfer/DataTransferPanel";
import { AutocompleteSearch } from "../features/externalMetadata/AutocompleteSearch";
import type { LiveMetadataDetails, MetadataProviderResolution } from "../features/externalMetadata/types";
import type { AppSettings, UpdateAppSettingsPayload } from "../features/settings/types";
import { collectifyClient } from "../shared/api/collectifyClient";
import "./App.css";

type LoadState = "idle" | "loading" | "ready" | "error";
type SettingsState = "idle" | "loading" | "ready" | "error";
type MetadataResolutionState = "idle" | "loading" | "ready" | "error";
type CollectionSortBy = "name" | "createdAt" | "personalRating";

type AttributeDraft = {
  key: string;
  label?: string;
  value: string;
  valueType?: string;
  unit?: string;
};

type ImagePreview = {
  name: string;
  url: string;
};

type VisibleItem = {
  collectionId: string;
  item: CollectionItem;
  rating?: number | null;
};

type SelectedItemContext = {
  collectionId: string;
  itemId: string;
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

const emptySettingsForm: UpdateAppSettingsPayload = {
  dataRootPath: "",
  theme: "System",
  automaticBackupEnabled: true,
  language: "it-IT"
};

function addMetadataValue(target: Record<string, string>, key: string, value: string | number | null | undefined) {
  if (value !== null && value !== undefined && String(value).trim()) {
    target[key] = String(value);
  }
}

function buildMetadataAttributeDrafts(details: LiveMetadataDetails, itemType: string): AttributeDraft[] {
  return [
    { key: "itemType", label: "Tipo item", value: itemType },
    { key: "originalTitle", label: "Titolo originale", value: details.originalTitle ?? "" },
    { key: "year", label: "Anno", value: details.year ?? "", valueType: "Number" },
    { key: "genres", label: "Generi", value: details.genres.join(", ") },
    { key: "posterUrl", label: "Poster", value: details.posterUrl ?? "", valueType: "Url" },
    { key: "backdropUrl", label: "Backdrop", value: details.backdropUrl ?? "", valueType: "Url" },
    {
      key: "runtimeMinutes",
      label: "Durata",
      value: details.runtimeMinutes?.toString() ?? "",
      valueType: "Number",
      unit: "min"
    },
    { key: "releaseDate", label: "Data uscita", value: details.releaseDate ?? "", valueType: "Date" },
    {
      key: "externalRating",
      label: "Rating esterno",
      value: details.externalRating?.toString() ?? "",
      valueType: "Number"
    },
    { key: "providerSourceId", label: "Provider source id", value: details.sourceId },
    { key: "providerSourceName", label: "Provider source name", value: details.sourceName }
  ].filter((attribute) => attribute.value.trim());
}

function mergeAttributeDrafts(current: AttributeDraft[], incoming: AttributeDraft[]) {
  const filledCurrent = current.filter((attribute) => attribute.key.trim() || attribute.value.trim());
  const next = [...filledCurrent];

  incoming.forEach((attribute) => {
    const existingIndex = next.findIndex((draft) => draft.key.trim().toLowerCase() === attribute.key.toLowerCase());

    if (existingIndex >= 0) {
      next[existingIndex] = attribute;
    } else {
      next.push(attribute);
    }
  });

  return next.length > 0 ? next : [{ key: "", value: "" }];
}

function getItemPreviewUrl(item: CollectionItem) {
  const primaryImage = item.images.find((image) => image.isPrimary) ?? item.images[0];
  if (primaryImage) {
    return collectifyClient.resolveAssetUrl(primaryImage.url);
  }

  const metadataImage = findMetadataImageUrl(item);
  return metadataImage ? collectifyClient.resolveAssetUrl(metadataImage) : null;
}

function findMetadataImageUrl(item: CollectionItem) {
  const metadataKeys = ["imageUrl", "posterUrl", "coverUrl", "artworkUrl", "coverArtArchive", "backdropUrl", "background"];

  for (const reference of item.externalReferences) {
    for (const key of metadataKeys) {
      const value = reference.metadata[key];
      if (isUsableImageUrl(value)) {
        return value;
      }
    }
  }

  for (const key of metadataKeys) {
    const value = item.attributes.find((attribute) => attribute.key.toLowerCase() === key.toLowerCase())?.value;
    if (isUsableImageUrl(value)) {
      return value;
    }
  }

  return null;
}

function isUsableImageUrl(value?: string | null) {
  return Boolean(value?.trim() && (value.startsWith("http://") || value.startsWith("https://") || value.startsWith("/")));
}

function getPersonalRating(item: CollectionItem) {
  const ratingAttribute = item.attributes.find((attribute) =>
    ["personalrating", "ratingpersonale", "valutazionepersonale"].includes(attribute.key.trim().toLowerCase())
  );
  const value = ratingAttribute?.value?.replace(',', '.');
  const parsed = value ? Number.parseFloat(value) : Number.NaN;

  return Number.isFinite(parsed) ? parsed : null;
}

function itemMatchesCollectionQuery(item: CollectionItem, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }

  const searchableText = [
    item.title,
    item.description,
    item.notes,
    item.condition,
    ...item.attributes.flatMap((attribute) => [attribute.label, attribute.value])
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();

  return searchableText.includes(normalizedQuery);
}

function sortCollectionItems(items: CollectionItem[], sortBy: CollectionSortBy) {
  return [...items].sort((first, second) => {
    if (sortBy === "createdAt") {
      return new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime();
    }

    if (sortBy === "personalRating") {
      return (getPersonalRating(second) ?? -1) - (getPersonalRating(first) ?? -1);
    }

    return first.title.localeCompare(second.title, "it", { sensitivity: "base" });
  });
}

function selectPreferredProvider(resolution: MetadataProviderResolution) {
  const availableProviders = resolution.providers.filter((provider) => provider.isAvailable);
  const tmdb = availableProviders.find((provider) => provider.providerId === "tmdb");

  if (resolution.macroCategory === "Cinema" && tmdb) {
    return tmdb.providerId;
  }

  return availableProviders.find((provider) => provider.isPrimary)?.providerId ?? availableProviders[0]?.providerId ?? "";
}

export function App() {
  const [collections, setCollections] = useState<CollectionSummary[]>([]);
  const [selectedCollection, setSelectedCollection] = useState<CollectionDetail | null>(null);
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [message, setMessage] = useState("Pronto");
  const [collectionModalOpen, setCollectionModalOpen] = useState(false);
  const [itemModalOpen, setItemModalOpen] = useState(false);
  const [selectedItemContext, setSelectedItemContext] = useState<SelectedItemContext | null>(null);
  const [collectionForm, setCollectionForm] = useState<CreateCollectionPayload>(emptyCollectionForm);
  const [itemForm, setItemForm] = useState<CreateItemPayload>(emptyItemForm);
  const [itemType, setItemType] = useState("");
  const [itemPersonalRating, setItemPersonalRating] = useState("");
  const [selectedMetadataProviderId, setSelectedMetadataProviderId] = useState("");
  const [attributeDrafts, setAttributeDrafts] = useState<AttributeDraft[]>([{ key: "", value: "" }]);
  const [itemImageFiles, setItemImageFiles] = useState<File[]>([]);
  const [itemImagePreviews, setItemImagePreviews] = useState<ImagePreview[]>([]);
  const [collectionQuickSearch, setCollectionQuickSearch] = useState("");
  const [collectionSortBy, setCollectionSortBy] = useState<CollectionSortBy>("name");
  const [settingsModalOpen, setSettingsModalOpen] = useState(false);
  const [settingsState, setSettingsState] = useState<SettingsState>("idle");
  const [settings, setSettings] = useState<AppSettings | null>(null);
  const [settingsForm, setSettingsForm] = useState<UpdateAppSettingsPayload>(emptySettingsForm);
  const [metadataResolution, setMetadataResolution] = useState<MetadataProviderResolution | null>(null);
  const [metadataResolutionState, setMetadataResolutionState] = useState<MetadataResolutionState>("idle");

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
    const previews = itemImageFiles.map((file) => ({
      name: file.name,
      url: URL.createObjectURL(file)
    }));

    setItemImagePreviews(previews);

    return () => {
      previews.forEach((preview) => URL.revokeObjectURL(preview.url));
    };
  }, [itemImageFiles]);

  useEffect(() => {
    if (!itemModalOpen || !selectedCollection) {
      setMetadataResolution(null);
      setMetadataResolutionState("idle");
      return;
    }

    const category = itemType || getDefaultItemType(selectedCollection.type);
    let cancelled = false;
    setMetadataResolutionState("loading");

    collectifyClient
      .resolveExternalMetadataProviders(category)
      .then((resolution) => {
        if (!cancelled) {
          setMetadataResolution(resolution);
          setSelectedMetadataProviderId((current) => {
            if (current && resolution.providers.some((provider) => provider.providerId === current && provider.isAvailable)) {
              return current;
            }

            return selectPreferredProvider(resolution);
          });
          setMetadataResolutionState("ready");
        }
      })
      .catch(() => {
        if (!cancelled) {
          setMetadataResolution(null);
          setSelectedMetadataProviderId("");
          setMetadataResolutionState("error");
        }
      });

    return () => {
      cancelled = true;
    };
  }, [itemModalOpen, itemType, selectedCollection?.type]);

  const totalItems = useMemo(
    () => collections.reduce((total, collection) => total + collection.itemCount, 0),
    [collections]
  );

  const visibleItems = useMemo<VisibleItem[]>(() => {
    const collectionItems = selectedCollection?.items
      .filter((item) => itemMatchesCollectionQuery(item, collectionQuickSearch)) ?? [];

    return sortCollectionItems(collectionItems, collectionSortBy).map((item) => ({
      collectionId: selectedCollection!.id,
      item,
      rating: getPersonalRating(item)
    }));
  }, [collectionQuickSearch, collectionSortBy, selectedCollection]);

  const selectedVisibleItem = useMemo(() => {
    if (!selectedItemContext) {
      return null;
    }

    return visibleItems.find(
      (visibleItem) =>
        visibleItem.collectionId === selectedItemContext.collectionId &&
        visibleItem.item.id === selectedItemContext.itemId
    ) ?? null;
  }, [selectedItemContext, visibleItems]);

  const itemTypeOptions = useMemo(() => {
    return selectedCollection ? itemTypeOptionsByCollectionType[selectedCollection.type] ?? [selectedCollection.type] : [];
  }, [selectedCollection]);

  function openNewItemModal() {
    if (!selectedCollection) {
      return;
    }

    setItemType(getDefaultItemType(selectedCollection.type));
    setItemForm(emptyItemForm);
    setItemPersonalRating("");
    setAttributeDrafts([{ key: "", value: "" }]);
    setItemImageFiles([]);
    setItemModalOpen(true);
  }

  function closeNewItemModal() {
    setItemModalOpen(false);
    setItemType("");
    setItemPersonalRating("");
    setSelectedMetadataProviderId("");
    setMetadataResolution(null);
  }

  function applyMetadataDetails(details: LiveMetadataDetails) {
    const metadata: Record<string, string> = {
      kind: details.kind,
      sourceName: details.sourceName
    };

    addMetadataValue(metadata, "originalTitle", details.originalTitle);
    addMetadataValue(metadata, "year", details.year);
    addMetadataValue(metadata, "imageUrl", details.posterUrl);
    addMetadataValue(metadata, "posterUrl", details.posterUrl);
    addMetadataValue(metadata, "backdropUrl", details.backdropUrl);
    addMetadataValue(metadata, "releaseDate", details.releaseDate);
    addMetadataValue(metadata, "externalRating", details.externalRating?.toString());

    setItemForm((current) => ({
      ...current,
      title: details.title,
      description: details.description ?? current.description,
      notes: details.externalUrl ? `Importato da ${details.sourceName}: ${details.externalUrl}` : current.notes,
      externalReferences: [
        {
          provider: details.provider,
          externalId: details.sourceId,
          url: details.externalUrl ?? undefined,
          metadata
        }
      ]
    }));

    setAttributeDrafts((current) => mergeAttributeDrafts(current, buildMetadataAttributeDrafts(details, itemType)));
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
      setMessage("Impostazioni salvate.");
    } catch {
      setMessage("Salvataggio impostazioni non riuscito.");
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
        label: attribute.label?.trim() || attribute.key.trim(),
        value: attribute.value.trim(),
        valueType: attribute.valueType ?? "Text",
        unit: attribute.unit
      }));

    if (itemType.trim() && !attributes.some((attribute) => attribute.key.toLowerCase() === "itemtype")) {
      attributes.push({
        key: "itemType",
        label: "Tipo item",
        value: itemType.trim(),
        valueType: "Text",
        unit: undefined
      });
    }

    if (itemPersonalRating.trim() && !attributes.some((attribute) => attribute.key.toLowerCase() === "personalrating")) {
      attributes.push({
        key: "personalRating",
        label: "Rating personale",
        value: itemPersonalRating.trim(),
        valueType: "Number",
        unit: undefined
      });
    }

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
      setItemType("");
      setItemPersonalRating("");
      setAttributeDrafts([{ key: "", value: "" }]);
      setItemImageFiles([]);
      setItemModalOpen(false);
      await loadCollections(selectedCollection.id);
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
      setMessage("Immagine sostituita.");
    } catch {
      setMessage("Sostituzione immagine non riuscita.");
    }
  }

  async function handleDeleteImage(collectionId: string, itemId: string, imageId: string) {
    try {
      await collectifyClient.deleteItemImage(collectionId, itemId, imageId);
      await loadCollections(collectionId);
      setMessage("Immagine eliminata.");
    } catch {
      setMessage("Eliminazione immagine non riuscita.");
    }
  }

  async function handleDeleteItem(collectionId: string, itemId: string) {
    const confirmed = window.confirm("Eliminare questo item dalla collezione?");
    if (!confirmed) {
      return;
    }

    try {
      await collectifyClient.deleteItem(collectionId, itemId);
      setSelectedItemContext(null);
      await loadCollections(collectionId);
      setMessage("Item eliminato dalla collezione.");
    } catch {
      setMessage("Eliminazione item non riuscita.");
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
              onClick={openNewItemModal}
            >
              <span aria-hidden="true">+</span>
              Nuovo elemento
            </button>
          </div>

          {selectedCollection && (
            <div className="collection-view-controls">
              <input
                value={collectionQuickSearch}
                onChange={(event) => setCollectionQuickSearch(event.target.value)}
                placeholder="Cerca in questa collezione"
              />
              <select value={collectionSortBy} onChange={(event) => setCollectionSortBy(event.target.value as CollectionSortBy)}>
                <option value="name">Ordine alfabetico</option>
                <option value="createdAt">Data di aggiunta</option>
                <option value="personalRating">Rating personale</option>
              </select>
            </div>
          )}

          {loadState === "error" && <div className="empty-state">Avvia il backend per leggere le collezioni locali.</div>}

          {loadState !== "error" && !selectedCollection && (
            <div className="empty-state">Crea una collezione dalla sidebar per iniziare il tuo archivio.</div>
          )}

          {selectedCollection && selectedCollection.items.length === 0 && (
            <div className="empty-state">Questa collezione e' ancora vuota. Aggiungi il primo elemento.</div>
          )}

          {selectedCollection && selectedCollection.items.length > 0 && visibleItems.length === 0 && (
            <div className="empty-state">Nessun elemento corrisponde alla ricerca in questa collezione.</div>
          )}

          <div className="items-grid">
            {visibleItems.map(({ collectionId, item }) => {
              const previewUrl = getItemPreviewUrl(item);

              return (
                <button
                  className="item-card"
                  key={item.id}
                  type="button"
                  onClick={() => setSelectedItemContext({ collectionId, itemId: item.id })}
                >
                  <div className="item-card__media" aria-hidden="true">
                    {previewUrl ? <img src={previewUrl} alt="" /> : item.title.slice(0, 1).toUpperCase()}
                  </div>
                  <div className="item-card__body">
                    <strong>{item.title}</strong>
                  </div>
                </button>
              );
            })}
          </div>
        </section>
      </main>

      {selectedVisibleItem && (
        <div className="modal-backdrop" role="presentation">
          <div className="modal modal--wide item-detail-modal" role="dialog" aria-modal="true">
            <div className="modal__header">
              <h2>{selectedVisibleItem.item.title}</h2>
              <button type="button" onClick={() => setSelectedItemContext(null)} aria-label="Chiudi">
                x
              </button>
            </div>
            <div className="item-detail-layout">
              <div className="item-detail-cover">
                {getItemPreviewUrl(selectedVisibleItem.item) ? (
                  <img src={getItemPreviewUrl(selectedVisibleItem.item)!} alt="" />
                ) : (
                  <span>{selectedVisibleItem.item.title.slice(0, 1).toUpperCase()}</span>
                )}
              </div>
              <div className="item-detail-content">
                <div className="result-meta">
                  <span>{selectedVisibleItem.item.condition}</span>
                  <span>Aggiunto {new Date(selectedVisibleItem.item.createdAt).toLocaleDateString("it-IT")}</span>
                  {getPersonalRating(selectedVisibleItem.item) !== null && (
                    <span>Rating {getPersonalRating(selectedVisibleItem.item)}</span>
                  )}
                </div>
                {selectedVisibleItem.item.description && <p>{selectedVisibleItem.item.description}</p>}
                {selectedVisibleItem.item.notes && <p>{selectedVisibleItem.item.notes}</p>}
                {selectedVisibleItem.item.attributes.length > 0 && (
                  <div className="attribute-list">
                    {selectedVisibleItem.item.attributes.map((attribute) => (
                      <span key={attribute.id}>
                        {attribute.label}: {attribute.value}
                      </span>
                    ))}
                  </div>
                )}
                {selectedVisibleItem.item.externalReferences.length > 0 && (
                  <div className="external-reference-list">
                    {selectedVisibleItem.item.externalReferences.map((reference) => (
                      <a
                        href={reference.url ?? "#"}
                        key={reference.id}
                        target="_blank"
                        rel="noreferrer"
                        aria-disabled={!reference.url}
                      >
                        {reference.provider}
                      </a>
                    ))}
                  </div>
                )}
              </div>
            </div>
            {selectedVisibleItem.item.images.length > 0 && (
              <div className="image-gallery">
                {selectedVisibleItem.item.images.map((image) => (
                  <div className="image-thumb" key={image.id}>
                    <img src={collectifyClient.resolveAssetUrl(image.url)} alt={image.caption ?? selectedVisibleItem.item.title} />
                    <div className="image-thumb__actions">
                      <label>
                        Sostituisci
                        <input
                          accept="image/gif,image/jpeg,image/png,image/webp"
                          type="file"
                          onChange={(event) =>
                            void handleReplaceImage(
                              selectedVisibleItem.collectionId,
                              selectedVisibleItem.item.id,
                              image.id,
                              event.currentTarget.files
                            )
                          }
                        />
                      </label>
                      <button
                        type="button"
                        onClick={() => void handleDeleteImage(selectedVisibleItem.collectionId, selectedVisibleItem.item.id, image.id)}
                      >
                        Elimina
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
            <div className="item-detail-actions">
              <label className="inline-upload">
                Aggiungi immagine
                <input
                  accept="image/gif,image/jpeg,image/png,image/webp"
                  multiple
                  type="file"
                  onChange={(event) =>
                    void handleUploadImages(selectedVisibleItem.collectionId, selectedVisibleItem.item.id, event.currentTarget.files)
                  }
                />
              </label>
              <button
                className="danger-action"
                type="button"
                onClick={() => void handleDeleteItem(selectedVisibleItem.collectionId, selectedVisibleItem.item.id)}
              >
                Elimina item
              </button>
            </div>
          </div>
        </div>
      )}

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
            <DataTransferPanel
              onDataImported={async () => {
                await loadCollections(selectedCollection?.id);
              }}
              onStatusMessage={setMessage}
            />
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
              <button type="button" onClick={closeNewItemModal} aria-label="Chiudi">
                x
              </button>
            </div>
            <div className="field-grid">
              <label>
                Tipo item
                <select value={itemType} onChange={(event) => setItemType(event.target.value)}>
                  {itemTypeOptions.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Titolo
                <AutocompleteSearch
                  itemType={itemType || selectedCollection.type}
                  providerId={selectedMetadataProviderId || undefined}
                  value={itemForm.title ?? ""}
                  onValueChange={(title) => setItemForm((current) => ({ ...current, title }))}
                  onSelect={applyMetadataDetails}
                  onStatusMessage={setMessage}
                  placeholder="Es. Dune"
                />
              </label>
            </div>
            <div className="metadata-resolution-panel">
              <span>Provider metadata</span>
              {metadataResolutionState === "loading" && <strong>Risoluzione in corso...</strong>}
              {metadataResolutionState === "error" && <strong>Resolver non disponibile</strong>}
              {metadataResolutionState === "ready" && metadataResolution && (
                <>
                  <strong>{metadataResolution.manualEntryOnly ? "Inserimento manuale" : metadataResolution.macroCategory}</strong>
                  {metadataResolution.providers.length > 0 && (
                    <div className="metadata-provider-list">
                      {metadataResolution.providers.map((provider) => (
                        <button
                          className={[
                            provider.isAvailable ? "is-available" : provider.isFuture ? "is-future" : "is-unavailable",
                            provider.providerId === selectedMetadataProviderId ? "is-selected" : ""
                          ].filter(Boolean).join(" ")}
                          disabled={!provider.isAvailable}
                          key={provider.providerId}
                          type="button"
                          onClick={() => setSelectedMetadataProviderId(provider.providerId)}
                        >
                          {provider.displayName}
                        </button>
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>
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
              <label>
                Rating personale
                <input
                  min="0"
                  max="10"
                  step="0.5"
                  type="number"
                  value={itemPersonalRating}
                  onChange={(event) => setItemPersonalRating(event.target.value)}
                  placeholder="0-10"
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
