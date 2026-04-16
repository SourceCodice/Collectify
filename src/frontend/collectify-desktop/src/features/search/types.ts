import type { CollectionItem } from "../collections/types";

export type LocalSearchQuery = {
  query?: string;
  collectionId?: string;
  categoryId?: string;
  tagId?: string;
  condition?: string;
  minRating?: string;
  maxRating?: string;
  dateFrom?: string;
  dateTo?: string;
  dateField?: "createdAt" | "updatedAt" | "acquiredAt";
  sortBy?: "name" | "createdAt" | "updatedAt" | "value";
  sortDirection?: "asc" | "desc";
};

export type LocalSearchResponse = {
  totalCount: number;
  items: LocalSearchResult[];
  facets: LocalSearchFacets;
};

export type LocalSearchResult = {
  collectionId: string;
  collectionName: string;
  collectionType: string;
  categoryId?: string | null;
  categoryName?: string | null;
  item: CollectionItem;
  tags: SearchOption[];
  rating?: number | null;
  value?: number | null;
};

export type LocalSearchFacets = {
  categories: SearchOption[];
  tags: SearchOption[];
  conditions: SearchOption[];
  ratingRange?: SearchRange | null;
  valueRange?: SearchRange | null;
};

export type SearchOption = {
  id: string;
  label: string;
  count: number;
};

export type SearchRange = {
  min: number;
  max: number;
};
