export type CollectionSummary = {
  id: string;
  name: string;
  type: string;
  description?: string | null;
  categoryId?: string | null;
  itemCount: number;
  updatedAt: string;
};

export type CollectionDetail = {
  id: string;
  name: string;
  type: string;
  description?: string | null;
  categoryId?: string | null;
  items: CollectionItem[];
  createdAt: string;
  updatedAt: string;
};

export type CollectionItem = {
  id: string;
  title: string;
  description?: string | null;
  notes?: string | null;
  condition: string;
  acquiredAt?: string | null;
  attributes: ItemAttribute[];
  tagIds: string[];
  images: ItemImage[];
  externalReferences: ExternalReference[];
  updatedAt: string;
};

export type ItemAttribute = {
  id: string;
  key: string;
  label: string;
  value: string;
  valueType: string;
  unit?: string | null;
};

export type ExternalReference = {
  id: string;
  provider: string;
  externalId?: string | null;
  url?: string | null;
  metadata: Record<string, string>;
};

export type ItemImage = {
  id: string;
  fileName: string;
  relativePath: string;
  url: string;
  contentType: string;
  sizeBytes: number;
  caption?: string | null;
  isPrimary: boolean;
  createdAt: string;
  updatedAt: string;
};

export type CreateCollectionPayload = {
  name: string;
  type: string;
  description?: string;
  categoryId?: string | null;
};

export type CreateItemPayload = {
  title: string;
  description?: string;
  notes?: string;
  condition?: string;
  acquiredAt?: string | null;
  attributes?: Array<{
    key: string;
    label: string;
    value: string;
    valueType?: string;
    unit?: string;
  }>;
  tagIds?: string[];
  externalReferences?: Array<{
    provider: string;
    externalId?: string;
    url?: string;
    metadata?: Record<string, string>;
  }>;
};
