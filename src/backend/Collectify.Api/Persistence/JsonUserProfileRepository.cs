using Collectify.Api.Modules.Collections;

namespace Collectify.Api.Persistence;

public sealed class JsonUserProfileRepository(ICollectifyDataStore dataStore) : IUserProfileRepository
{
    public async Task<UserProfile> GetAsync(CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        return document.UserProfile;
    }

    public async Task<UserProfile> SaveAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var document = await dataStore.LoadAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        profile.Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id;
        profile.CreatedAt = profile.CreatedAt == default ? now : profile.CreatedAt;
        profile.UpdatedAt = now;

        document.UserProfile = profile;
        await dataStore.SaveAsync(document, cancellationToken);

        return profile;
    }
}
