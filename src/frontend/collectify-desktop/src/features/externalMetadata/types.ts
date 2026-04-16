import type { CollectionItem } from "../collections/types";

export type ExternalMetadataKind = "movie" | "game" | "album";

export type ExternalMetadataSearchResult = {
  provider: string;
  kind: ExternalMetadataKind;
  externalId: string;
  title: string;
  subtitle?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  releaseDate?: string | null;
  externalUrl?: string | null;
  metadata: Record<string, string>;
};

export type ExternalMetadataDetails = {
  provider: string;
  kind: ExternalMetadataKind;
  externalId: string;
  title: string;
  description?: string | null;
  imageUrl?: string | null;
  releaseDate?: string | null;
  externalUrl?: string | null;
  attributes: ExternalMetadataAttribute[];
  metadata: Record<string, string>;
};

export type ExternalMetadataAttribute = {
  key: string;
  label: string;
  value: string;
  valueType: string;
  unit?: string | null;
};

export type ImportExternalItemPayload = {
  collectionId: string;
  provider: string;
  externalId: string;
};

export type ImportedExternalItem = CollectionItem;
