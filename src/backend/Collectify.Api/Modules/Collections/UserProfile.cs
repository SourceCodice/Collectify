namespace Collectify.Api.Modules.Collections;

public sealed class UserProfile
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = "Collectify User";
    public string? Email { get; set; }
    public string PreferredLanguage { get; set; } = "it-IT";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
