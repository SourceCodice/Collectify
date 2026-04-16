import type {
  CollectionDetail,
  ItemImage,
  CollectionItem,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../../features/collections/types";
import type {
  ExternalMetadataDetails,
  ExternalMetadataKind,
  ExternalMetadataSearchResult,
  ImportExternalItemPayload,
  ImportedExternalItem
} from "../../features/externalMetadata/types";

const apiBaseUrl = import.meta.env.VITE_COLLECTIFY_API_URL ?? "http://localhost:5088";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    throw new Error(`Collectify API returned ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function uploadImage<T>(path: string, formData: FormData, method: "POST" | "PUT"): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    body: formData
  });

  if (!response.ok) {
    throw new Error(`Collectify API returned ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function requestNoContent(path: string, init?: RequestInit): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${path}`, init);

  if (!response.ok) {
    throw new Error(`Collectify API returned ${response.status}`);
  }
}

function buildImageFormData(file: File, options?: { caption?: string; isPrimary?: boolean }) {
  const formData = new FormData();
  formData.append("file", file);

  if (options?.caption) {
    formData.append("caption", options.caption);
  }

  if (typeof options?.isPrimary === "boolean") {
    formData.append("isPrimary", String(options.isPrimary));
  }

  return formData;
}

function externalMetadataPath(kind: ExternalMetadataKind) {
  return kind === "movie" ? "movies" : kind === "game" ? "games" : "albums";
}

export const collectifyClient = {
  apiBaseUrl,
  resolveAssetUrl: (url: string) => (url.startsWith("http") ? url : `${apiBaseUrl}${url}`),
  listCollections: () => request<CollectionSummary[]>("/api/collections"),
  getCollection: (id: string) => request<CollectionDetail>(`/api/collections/${id}`),
  createCollection: (payload: CreateCollectionPayload) =>
    request<CollectionDetail>("/api/collections", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  addItem: (collectionId: string, payload: CreateItemPayload) =>
    request<CollectionItem>(`/api/collections/${collectionId}/items`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  uploadItemImage: (collectionId: string, itemId: string, file: File, options?: { caption?: string; isPrimary?: boolean }) =>
    uploadImage<ItemImage>(
      `/api/collections/${collectionId}/items/${itemId}/images`,
      buildImageFormData(file, options),
      "POST"
    ),
  replaceItemImage: (
    collectionId: string,
    itemId: string,
    imageId: string,
    file: File,
    options?: { caption?: string; isPrimary?: boolean }
  ) =>
    uploadImage<ItemImage>(
      `/api/collections/${collectionId}/items/${itemId}/images/${imageId}`,
      buildImageFormData(file, options),
      "PUT"
    ),
  deleteItemImage: (collectionId: string, itemId: string, imageId: string) =>
    requestNoContent(`/api/collections/${collectionId}/items/${itemId}/images/${imageId}`, {
      method: "DELETE"
    }),
  searchExternalMetadata: (kind: ExternalMetadataKind, query: string) =>
    request<ExternalMetadataSearchResult[]>(
      `/api/external/${externalMetadataPath(kind)}/search?query=${encodeURIComponent(query)}`
    ),
  getExternalMetadataDetails: (kind: ExternalMetadataKind, externalId: string) =>
    request<ExternalMetadataDetails>(
      `/api/external/${externalMetadataPath(kind)}/${encodeURIComponent(externalId)}`
    ),
  importExternalItem: (payload: ImportExternalItemPayload) =>
    request<ImportedExternalItem>("/api/external/import", {
      method: "POST",
      body: JSON.stringify(payload)
    })
};
