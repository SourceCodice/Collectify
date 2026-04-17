import type { CollectionItem } from "../collections/types";

export type ExternalMetadataProviderKind = "movie" | "game" | "album";
export type ExternalMetadataKind = ExternalMetadataProviderKind | "show" | "single" | "book" | "manual";

export type MetadataProviderRole = "primary" | "optional" | "future";

export type MetadataProviderCapability = {
  providerId: string;
  displayName: string;
  kind: ExternalMetadataKind;
  role: MetadataProviderRole;
  isPrimary: boolean;
  isOptional: boolean;
  isFuture: boolean;
  isEnabled: boolean;
  isRegistered: boolean;
  isConfigured: boolean;
  isAvailable: boolean;
  supportsSearch: boolean;
  supportsDetails: boolean;
  notes?: string | null;
};

export type MetadataProviderResolution = {
  requestedCategory: string;
  macroCategory: string;
  manualEntryOnly: boolean;
  aliases: string[];
  providers: MetadataProviderCapability[];
};

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

export type LiveMetadataSearchResult = ExternalMetadataSearchResult & {
  providerName: string;
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

export type LiveMetadataDetails = ExternalMetadataDetails & {
  providerName: string;
  originalTitle?: string | null;
  year?: string | null;
  genres: string[];
  posterUrl?: string | null;
  backdropUrl?: string | null;
  runtimeMinutes?: number | null;
  externalRating?: number | null;
  sourceId: string;
  sourceName: string;
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
