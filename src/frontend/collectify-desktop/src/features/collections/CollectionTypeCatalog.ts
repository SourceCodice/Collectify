export const collectionTypes = [
  "Movies",
  "Videogames",
  "Books",
  "Cars",
  "Plants",
  "Furniture",
  "Custom"
] as const;

export const collectionTypeLabels: Record<string, string> = {
  Movies: "Film",
  Videogames: "Videogiochi",
  Books: "Libri",
  Cars: "Auto",
  Plants: "Piante",
  Furniture: "Mobili",
  Custom: "Personalizzata"
};
