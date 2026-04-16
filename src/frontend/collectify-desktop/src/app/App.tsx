import { FormEvent, useEffect, useMemo, useState } from "react";
import { CollectionCard } from "../features/collections/CollectionCard";
import { collectionTypeLabels, collectionTypes } from "../features/collections/CollectionTypeCatalog";
import type { CollectionSummary, CreateCollectionPayload } from "../features/collections/types";
import { collectifyClient } from "../shared/api/collectifyClient";
import "./App.css";

type LoadState = "idle" | "loading" | "ready" | "error";

const emptyForm: CreateCollectionPayload = {
  name: "",
  type: "Custom",
  description: ""
};

export function App() {
  const [collections, setCollections] = useState<CollectionSummary[]>([]);
  const [form, setForm] = useState<CreateCollectionPayload>(emptyForm);
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [message, setMessage] = useState<string>("Pronto");

  async function loadCollections() {
    setLoadState("loading");

    try {
      const data = await collectifyClient.listCollections();
      setCollections(data);
      setLoadState("ready");
      setMessage(`API collegata: ${collectifyClient.apiBaseUrl}`);
    } catch {
      setLoadState("error");
      setMessage(`API non raggiungibile: ${collectifyClient.apiBaseUrl}`);
    }
  }

  useEffect(() => {
    void loadCollections();
  }, []);

  const totalItems = useMemo(
    () => collections.reduce((total, collection) => total + collection.itemCount, 0),
    [collections]
  );

  async function handleCreateCollection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!form.name.trim()) {
      setMessage("Inserisci un nome per la collezione.");
      return;
    }

    try {
      await collectifyClient.createCollection(form);
      setForm(emptyForm);
      await loadCollections();
      setMessage("Collezione creata.");
    } catch {
      setMessage("Creazione non riuscita. Controlla che il backend sia avviato.");
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-mark" aria-hidden="true">
          C
        </div>
        <div>
          <span className="eyebrow">Collectify</span>
          <h1>Archivio personale</h1>
        </div>
        <div className={`connection-pill connection-pill--${loadState}`}>
          <span aria-hidden="true" />
          {message}
        </div>
      </header>

      <main className="workspace">
        <section className="overview-panel" aria-label="Panoramica">
          <div>
            <p className="eyebrow">Dashboard</p>
            <h2>Organizza ogni collezione con lo stesso metodo.</h2>
          </div>
          <div className="stats-grid">
            <div>
              <strong>{collections.length}</strong>
              <span>collezioni</span>
            </div>
            <div>
              <strong>{totalItems}</strong>
              <span>oggetti</span>
            </div>
            <div>
              <strong>{window.collectify?.platform ?? "web"}</strong>
              <span>runtime</span>
            </div>
          </div>
        </section>

        <section className="content-grid">
          <form className="collection-form" onSubmit={handleCreateCollection}>
            <div className="form-header">
              <span className="eyebrow">Nuova</span>
              <h2>Collezione</h2>
            </div>

            <label>
              Nome
              <input
                value={form.name}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                placeholder="Es. Garage dei sogni"
              />
            </label>

            <label>
              Tipo
              <select
                value={form.type}
                onChange={(event) => setForm((current) => ({ ...current, type: event.target.value }))}
              >
                {collectionTypes.map((type) => (
                  <option key={type} value={type}>
                    {collectionTypeLabels[type]}
                  </option>
                ))}
              </select>
            </label>

            <label>
              Note
              <textarea
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                placeholder="Criteri, stato, desiderata o provenienza."
                rows={5}
              />
            </label>

            <button type="submit">
              <span aria-hidden="true">+</span>
              Crea
            </button>
          </form>

          <section className="collections-area" aria-label="Collezioni">
            <div className="section-heading">
              <div>
                <p className="eyebrow">Raccolte</p>
                <h2>Le tue collezioni</h2>
              </div>
              <button className="ghost-button" type="button" onClick={() => void loadCollections()}>
                Aggiorna
              </button>
            </div>

            {loadState === "loading" && <div className="empty-state">Caricamento...</div>}

            {loadState === "error" && (
              <div className="empty-state">
                Avvia il backend su <strong>{collectifyClient.apiBaseUrl}</strong>.
              </div>
            )}

            {loadState !== "loading" && loadState !== "error" && collections.length === 0 && (
              <div className="empty-state">Nessuna collezione presente.</div>
            )}

            <div className="collections-grid">
              {collections.map((collection) => (
                <CollectionCard key={collection.id} collection={collection} />
              ))}
            </div>
          </section>
        </section>
      </main>
    </div>
  );
}
