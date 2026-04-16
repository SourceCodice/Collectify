import type { CollectionSummary, CreateCollectionPayload } from "../../features/collections/types";

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
  createCollection: (payload: CreateCollectionPayload) =>
    request<CollectionSummary>("/api/collections", {
      method: "POST",
      body: JSON.stringify(payload)
    })
};
