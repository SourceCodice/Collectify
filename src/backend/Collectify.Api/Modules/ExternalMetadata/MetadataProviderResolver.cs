using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class MetadataProviderResolver(
    IEnumerable<IExternalMetadataProvider> providers,
    IOptions<ExternalMetadataOptions> options,
    ILogger<MetadataProviderResolver> logger)
{
    private readonly IReadOnlyDictionary<string, IExternalMetadataProvider> _providers = providers
        .ToDictionary(provider => provider.ProviderId, StringComparer.OrdinalIgnoreCase);

    public MetadataProviderResolutionResponse Resolve(string itemTypeOrMacroCategory)
    {
        var requestedCategory = NormalizeCategory(itemTypeOrMacroCategory);
        var category = ResolveCategory(requestedCategory);

        if (category is null)
        {
            logger.LogDebug("No metadata provider mapping found for category {Category}.", requestedCategory);
            return new MetadataProviderResolutionResponse(requestedCategory, requestedCategory, true, [], []);
        }

        var capabilities = category.Options.Providers
            .Select(providerReference => BuildCapability(providerReference))
            .ToList();

        return new MetadataProviderResolutionResponse(
            requestedCategory,
            category.Name,
            category.Options.ManualEntryOnly || capabilities.Count == 0 || capabilities.All(provider => !provider.IsAvailable),
            category.Options.Aliases,
            capabilities);
    }

    public IReadOnlyList<IExternalMetadataProvider> ResolveAvailableProviders(string itemTypeOrMacroCategory)
    {
        var resolution = Resolve(itemTypeOrMacroCategory);

        return resolution.Providers
            .Where(provider => provider.IsAvailable && provider.SupportsSearch)
            .Select(provider => _providers[provider.ProviderId])
            .ToList();
    }

    public IExternalMetadataProvider? ResolveAvailableProvider(string itemTypeOrMacroCategory, string providerId)
    {
        var capability = ResolveCapability(itemTypeOrMacroCategory, providerId);
        return capability?.IsAvailable == true && _providers.TryGetValue(capability.ProviderId, out var provider)
            ? provider
            : null;
    }

    public MetadataProviderCapability? ResolveCapability(string itemTypeOrMacroCategory, string providerId)
    {
        var normalizedProviderId = NormalizeProviderId(providerId);
        return Resolve(itemTypeOrMacroCategory).Providers.FirstOrDefault(provider =>
            string.Equals(provider.ProviderId, normalizedProviderId, StringComparison.OrdinalIgnoreCase));
    }

    private ResolvedCategory? ResolveCategory(string category)
    {
        foreach (var item in options.Value.ProviderResolver.Categories)
        {
            if (string.Equals(item.Key, category, StringComparison.OrdinalIgnoreCase) ||
                item.Value.Aliases.Any(alias => string.Equals(alias, category, StringComparison.OrdinalIgnoreCase)))
            {
                return new ResolvedCategory(item.Key, item.Value);
            }
        }

        return null;
    }

    private MetadataProviderCapability BuildCapability(MetadataProviderReferenceOptions providerReference)
    {
        var providerId = NormalizeProviderId(providerReference.ProviderId);
        var role = NormalizeRole(providerReference.Role);
        var isRegistered = _providers.TryGetValue(providerId, out var registeredProvider);
        var isConfigured = IsConfigured(providerId);
        var isFuture = string.Equals(role, MetadataProviderRoles.Future, StringComparison.OrdinalIgnoreCase);
        var isEnabled = providerReference.IsEnabled && !isFuture;
        var isAvailable = isEnabled && isRegistered && isConfigured;

        return new MetadataProviderCapability(
            providerId,
            string.IsNullOrWhiteSpace(providerReference.DisplayName) ? providerId.ToUpperInvariant() : providerReference.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(providerReference.Kind) ? registeredProvider?.SupportedKind ?? ExternalMetadataKinds.Manual : providerReference.Kind.Trim(),
            role,
            string.Equals(role, MetadataProviderRoles.Primary, StringComparison.OrdinalIgnoreCase),
            string.Equals(role, MetadataProviderRoles.Optional, StringComparison.OrdinalIgnoreCase),
            isFuture,
            isEnabled,
            isRegistered,
            isConfigured,
            isAvailable,
            isAvailable,
            isAvailable,
            providerReference.Notes);
    }

    private bool IsConfigured(string providerId)
    {
        var metadataOptions = options.Value;

        return providerId.ToLowerInvariant() switch
        {
            "tmdb" => !string.IsNullOrWhiteSpace(metadataOptions.TMDb.ApiKey) ||
                !string.IsNullOrWhiteSpace(metadataOptions.TMDb.AccessToken),
            "rawg" => !string.IsNullOrWhiteSpace(metadataOptions.RAWG.ApiKey),
            "discogs" => !string.IsNullOrWhiteSpace(metadataOptions.Discogs.ApiKey) &&
                !string.IsNullOrWhiteSpace(metadataOptions.Discogs.ApiSecret),
            _ => false
        };
    }

    private static string NormalizeCategory(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeProviderId(string providerId)
    {
        return string.IsNullOrWhiteSpace(providerId) ? string.Empty : providerId.Trim().ToLowerInvariant();
    }

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role) ? MetadataProviderRoles.Primary : role.Trim().ToLowerInvariant();
    }

    private sealed record ResolvedCategory(string Name, MetadataCategoryProviderOptions Options);
}
