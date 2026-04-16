using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public static class CollectifySeedData
{
    public static CollectifyDataDocument Create()
    {
        var now = DateTimeOffset.UtcNow;
        var moviesCategoryId = Guid.Parse("18025723-e42e-4eaf-a107-378cd3a8b52f");
        var plantsCategoryId = Guid.Parse("29d93270-9f2f-4b70-a0df-6fb6cde31a73");
        var favoriteTagId = Guid.Parse("84101b63-baf6-4dbc-89d4-e27966b2b9d8");

        return new CollectifyDataDocument
        {
            SchemaVersion = 1,
            UserProfile = new UserProfile
            {
                Id = Guid.Parse("8b6ff3af-e7d0-4e21-bcb4-80ec7ab293d6"),
                DisplayName = "Collectify User",
                PreferredLanguage = "it-IT",
                CreatedAt = now,
                UpdatedAt = now
            },
            AppSettings = new AppSettings
            {
                Id = Guid.Parse("08e195f8-46f5-43c1-8e5c-47ccff4e93d8"),
                DataRootPath = string.Empty,
                Theme = "System",
                AutomaticBackupEnabled = true,
                Language = "it-IT",
                Locale = "it-IT",
                Currency = "EUR",
                DateFormat = "dd/MM/yyyy",
                DataSchemaVersion = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            Categories =
            [
                new CollectionCategory
                {
                    Id = moviesCategoryId,
                    Name = "Film",
                    Description = "Film fisici e digitali.",
                    Icon = "film",
                    Color = "#184d84",
                    SortOrder = 10,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new CollectionCategory
                {
                    Id = plantsCategoryId,
                    Name = "Piante",
                    Description = "Piante, cure e note stagionali.",
                    Icon = "leaf",
                    Color = "#2f7d4e",
                    SortOrder = 20,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ],
            Tags =
            [
                new Tag
                {
                    Id = favoriteTagId,
                    Name = "Preferito",
                    Color = "#7b2745",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ],
            Collections =
            [
                new Collection
                {
                    Id = Guid.Parse("8a0bb4c6-26f7-4c7d-9cd5-5f25a9c75f01"),
                    CategoryId = moviesCategoryId,
                    Name = "Film preferiti",
                    Type = "Movies",
                    Description = "Blu-ray, DVD e film digitali da tenere d'occhio.",
                    CreatedAt = now,
                    UpdatedAt = now,
                    Items =
                    [
                        new Item
                        {
                            Id = Guid.Parse("c7e1b785-f255-468d-95e7-994a38f24b8f"),
                            CollectionId = Guid.Parse("8a0bb4c6-26f7-4c7d-9cd5-5f25a9c75f01"),
                            Title = "Blade Runner",
                            Notes = "Director's cut",
                            Condition = "Ottimo",
                            AcquiredAt = now,
                            TagIds = [favoriteTagId],
                            Attributes =
                            [
                                new ItemAttribute
                                {
                                    Id = Guid.Parse("1d273f19-186c-4ba5-933e-aa59af4de8a4"),
                                    Key = "format",
                                    Label = "Formato",
                                    Value = "Blu-ray",
                                    ValueType = "Text",
                                    CreatedAt = now,
                                    UpdatedAt = now
                                }
                            ],
                            ExternalReferences =
                            [
                                new ExternalReference
                                {
                                    Id = Guid.Parse("740c0289-9474-4430-89ef-84ecfe2cd265"),
                                    Provider = "IMDb",
                                    ExternalId = "tt0083658",
                                    Url = "https://www.imdb.com/title/tt0083658/",
                                    CreatedAt = now,
                                    UpdatedAt = now
                                }
                            ],
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    ]
                },
                new Collection
                {
                    Id = Guid.Parse("2407d0c8-cbd3-41d8-bc1d-8213978b7e41"),
                    CategoryId = plantsCategoryId,
                    Name = "Piante di casa",
                    Type = "Plants",
                    Description = "Specie, cure e note sulle ultime annaffiature.",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            ],
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
