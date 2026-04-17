import type {
  CollectionDetail,
  ItemImage,
  CollectionItem,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../../features/collections/types";
import type { DataBackupResponse, DataImportResponse } from "../../features/dataTransfer/types";
import type {
  ExternalMetadataDetails,
  ExternalMetadataProviderKind,
  ExternalMetadataSearchResult,
  ImportExternalItemPayload,
  ImportedExternalItem,
  LiveMetadataDetails,
  LiveMetadataSearchResult,
  MetadataProviderResolution
} from "../../features/externalMetadata/types";
import type { LocalSearchQuery, LocalSearchResponse } from "../../features/search/types";
import type { AppSettings, UpdateAppSettingsPayload } from "../../features/settings/types";

const apiBaseUrl = import.meta.env.VITE_COLLECTIFY_API_URL ?? "http://localhost:5088";

async function readErrorMessage(response: Response) {
  try {
    const problem = await response.json();
    const validationErrors = problem?.errors
      ? Object.values(problem.errors)
          .flat()
          .filter(Boolean)
      : [];

    if (validationErrors.length > 0) {
      return validationErrors.join(" ");
    }

    return problem?.detail || problem?.title || `Collectify API returned ${response.status}`;
  } catch {
    return `Collectify API returned ${response.status}`;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  return response.json() as Promise<T>;
}

async function uploadImage<T>(path: string, formData: FormData, method: "POST" | "PUT"): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    body: formData
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  return response.json() as Promise<T>;
}

async function requestNoContent(path: string, init?: RequestInit): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${path}`, init);

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }
}

async function uploadDataImport(path: string, file: File): Promise<DataImportResponse> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    body: formData
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  return response.json() as Promise<DataImportResponse>;
}

async function downloadExportFile(path: string): Promise<string> {
  const response = await fetch(`${apiBaseUrl}${path}`);

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = objectUrl;
  anchor.download = readFileName(response.headers.get("Content-Disposition")) ?? "collectify-export.json";
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);

  return anchor.download;
}

function readFileName(contentDisposition: string | null) {
  if (!contentDisposition) {
    return null;
  }

  const encodedMatch = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (encodedMatch?.[1]) {
    return decodeURIComponent(encodedMatch[1].replace(/"/g, ""));
  }

  const plainMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
  return plainMatch?.[1] ?? null;
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

function externalMetadataPath(kind: ExternalMetadataProviderKind) {
  return kind === "movie" ? "movies" : kind === "game" ? "games" : "albums";
}

function buildQueryString(params: Record<string, string | undefined>) {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value) {
      searchParams.set(key, value);
    }
  });

  return searchParams.toString();
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
  deleteItem: (collectionId: string, itemId: string) =>
    requestNoContent(`/api/collections/${collectionId}/items/${itemId}`, {
      method: "DELETE"
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
  resolveExternalMetadataProviders: (itemTypeOrMacroCategory: string) =>
    request<MetadataProviderResolution>(
      `/api/external/providers?itemType=${encodeURIComponent(itemTypeOrMacroCategory)}`
    ),
  searchExternalMetadataForCategory: (macroCategory: string, query: string) =>
    request<ExternalMetadataSearchResult[]>(
      `/api/external/search?macroCategory=${encodeURIComponent(macroCategory)}&query=${encodeURIComponent(query)}`
    ),
  searchLiveExternalMetadata: (itemType: string, query: string, providerId?: string, signal?: AbortSignal) =>
    request<LiveMetadataSearchResult[]>(
      `/api/external/live/search?itemType=${encodeURIComponent(itemType)}&query=${encodeURIComponent(query)}${providerId ? `&provider=${encodeURIComponent(providerId)}` : ""}`,
      { signal }
    ),
  getLiveExternalMetadataDetails: (itemType: string, provider: string, externalId: string, signal?: AbortSignal) =>
    request<LiveMetadataDetails>(
      `/api/external/live/details?itemType=${encodeURIComponent(itemType)}&provider=${encodeURIComponent(provider)}&externalId=${encodeURIComponent(externalId)}`,
      { signal }
    ),
  searchExternalMetadata: (kind: ExternalMetadataProviderKind, query: string) =>
    request<ExternalMetadataSearchResult[]>(
      `/api/external/${externalMetadataPath(kind)}/search?query=${encodeURIComponent(query)}`
    ),
  getExternalMetadataDetails: (kind: ExternalMetadataProviderKind, externalId: string) =>
    request<ExternalMetadataDetails>(
      `/api/external/${externalMetadataPath(kind)}/${encodeURIComponent(externalId)}`
    ),
  importExternalItem: (payload: ImportExternalItemPayload) =>
    request<ImportedExternalItem>("/api/external/import", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  searchItems: (query: LocalSearchQuery) => {
    const queryString = buildQueryString(query);
    return request<LocalSearchResponse>(`/api/search/items${queryString ? `?${queryString}` : ""}`);
  },
  getSettings: () => request<AppSettings>("/api/settings"),
  updateSettings: (payload: UpdateAppSettingsPayload) =>
    request<AppSettings>("/api/settings", {
      method: "PUT",
      body: JSON.stringify(payload)
    }),
  createBackup: () =>
    request<DataBackupResponse>("/api/data/backup", {
      method: "POST"
    }),
  exportData: () => downloadExportFile("/api/data/export"),
  importData: (file: File) => uploadDataImport("/api/data/import", file)
};
