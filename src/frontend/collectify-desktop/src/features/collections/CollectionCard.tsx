import { collectionTypeLabels } from "./CollectionTypeCatalog";
import type { CollectionSummary } from "./types";

type CollectionCardProps = {
  collection: CollectionSummary;
};

export function CollectionCard({ collection }: CollectionCardProps) {
  const updatedAt = new Intl.DateTimeFormat("it-IT", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(new Date(collection.updatedAt));

  return (
    <article className="collection-card">
      <div className="collection-card__header">
        <span className="collection-card__type">
          {collectionTypeLabels[collection.type] ?? collection.type}
        </span>
        <span className="collection-card__count">{collection.itemCount}</span>
      </div>
      <h3>{collection.name}</h3>
      <p>{collection.description ?? "Nessuna descrizione inserita."}</p>
      <footer>Aggiornata {updatedAt}</footer>
    </article>
  );
}
