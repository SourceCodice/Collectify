export type CollectionSummary = {
  id: string;
  name: string;
  type: string;
  description?: string | null;
  categoryId?: string | null;
  itemCount: number;
  updatedAt: string;
};

export type CreateCollectionPayload = {
  name: string;
  type: string;
  description?: string;
};
