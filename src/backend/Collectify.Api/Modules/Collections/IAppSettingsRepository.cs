namespace Collectify.Api.Modules.Collections;

public interface IAppSettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);
    Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
