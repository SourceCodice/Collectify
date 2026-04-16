namespace Collectify.Api.Modules.ExternalMetadata;

public sealed class ExternalMetadataProviderException(string message, int statusCode = StatusCodes.Status502BadGateway)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
