import type {
  CollectionDetail,
  CollectionItem,
  CollectionSummary,
  CreateCollectionPayload,
  CreateItemPayload
} from "../../features/collections/types";

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

export const collectifyClient = {
  apiBaseUrl,
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
    })
};
