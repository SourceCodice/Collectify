export const collectionTypes = [
  "Movies",
  "Videogames",
  "Music",
  "Books",
  "Cars",
  "Plants",
  "Furniture",
  "Custom"
] as const;

export const collectionTypeLabels: Record<string, string> = {
  Movies: "Film",
  Videogames: "Videogiochi",
  Music: "Musica",
  Books: "Libri",
  Cars: "Auto",
  Plants: "Piante",
  Furniture: "Mobili",
  Custom: "Personalizzata"
};

export const itemTypeOptionsByCollectionType: Record<string, string[]> = {
  Movies: ["Blu-ray", "DVD", "4K UHD", "Digitale"],
  Videogames: ["Videogioco", "Collector edition", "DLC"],
  Music: ["Vinile", "CD", "Album digitale", "Singolo"],
  Books: ["Libro", "Fumetto", "Manga"],
  Cars: ["Auto"],
  Plants: ["Pianta"],
  Furniture: ["Arredo"],
  Custom: ["Oggetto"]
};

export function getDefaultItemType(collectionType: string) {
  return itemTypeOptionsByCollectionType[collectionType]?.[0] ?? collectionType;
}
