using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class JsonAppSettingsRepository(ICollectifyDataStore dataStore) : IAppSettingsRepository
{
    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        return document.AppSettings;
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        settings.Id = settings.Id == Guid.Empty ? Guid.NewGuid() : settings.Id;
        settings.CreatedAt = settings.CreatedAt == default ? now : settings.CreatedAt;
        settings.UpdatedAt = now;

        document.AppSettings = settings;
        await dataStore.SaveAsync(document, cancellationToken);

        return settings;
    }
}
