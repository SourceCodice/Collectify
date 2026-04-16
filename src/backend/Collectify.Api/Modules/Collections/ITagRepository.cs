namespace Collectify.Api.Modules.Collections;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> ListAsync(CancellationToken cancellationToken);
    Task<Tag?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Tag> SaveAsync(Tag tag, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
