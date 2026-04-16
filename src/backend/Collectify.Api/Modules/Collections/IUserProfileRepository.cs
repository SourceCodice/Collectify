namespace Collectify.Api.Modules.Collections;

public interface IUserProfileRepository
{
    Task<UserProfile> GetAsync(CancellationToken cancellationToken);
    Task<UserProfile> SaveAsync(UserProfile profile, CancellationToken cancellationToken);
}
