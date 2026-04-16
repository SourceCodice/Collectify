using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Collectify.Api.Modules.ExternalMetadata;

public abstract class ExternalMetadataProviderBase(IHttpClientFactory httpClientFactory, IOptions<ExternalMetadataOptions> options)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _rateGate = new(1, 1);
    private DateTimeOffset _nextAllowedRequestAt = DateTimeOffset.MinValue;

    protected ExternalMetadataOptions Options => options.Value;

    protected async Task<T?> GetJsonAsync<T>(
        string url,
        ExternalMetadataProviderOptions providerOptions,
        Action<HttpRequestHeaders>? configureHeaders,
        CancellationToken cancellationToken)
    {
        await WaitForRateLimitAsync(providerOptions, cancellationToken);

        var client = httpClientFactory.CreateClient("ExternalMetadata");
        var maxRetries = Math.Max(0, Options.Retry.MaxRetries);
        var delay = TimeSpan.FromMilliseconds(Math.Max(100, Options.Retry.InitialDelayMilliseconds));

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            configureHeaders?.Invoke(request.Headers);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
            }

            if (!ShouldRetry(response.StatusCode) || attempt == maxRetries)
            {
                throw new ExternalMetadataProviderException($"External provider returned {(int)response.StatusCode}.");
            }

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        }

        return default;
    }

    protected static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    protected static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    protected static Dictionary<string, string> Metadata(params (string Key, string? Value)[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value.Value))
            .ToDictionary(value => value.Key, value => value.Value!.Trim());
    }

    protected static string BuildUrl(string baseUrl, string path, params (string Key, string? Value)[] query)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var normalizedPath = path.TrimStart('/');
        var queryString = string.Join(
            "&",
            query
                .Where(value => !string.IsNullOrWhiteSpace(value.Value))
                .Select(value => $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(value.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString)
            ? $"{normalizedBaseUrl}/{normalizedPath}"
            : $"{normalizedBaseUrl}/{normalizedPath}?{queryString}";
    }

    private async Task WaitForRateLimitAsync(ExternalMetadataProviderOptions providerOptions, CancellationToken cancellationToken)
    {
        var requestsPerSecond = Math.Max(1, providerOptions.RequestsPerSecond);
        var interval = TimeSpan.FromSeconds(1d / requestsPerSecond);

        await _rateGate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (_nextAllowedRequestAt > now)
            {
                await Task.Delay(_nextAllowedRequestAt - now, cancellationToken);
            }

            _nextAllowedRequestAt = DateTimeOffset.UtcNow.Add(interval);
        }
        finally
        {
            _rateGate.Release();
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    }
}
