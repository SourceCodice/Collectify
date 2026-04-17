namespace Collectify.Api.Modules.DataTransfer;

public static class DataTransferEndpoints
{
    public static IEndpointRouteBuilder MapDataTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/data")
            .WithTags("Data");

        group.MapPost("/backup", async (DataTransferApplicationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await service.CreateBackupAsync(cancellationToken);
                return Results.Ok(response);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return DataTransferProblem(
                    "Backup non riuscito",
                    "Collectify non riesce a creare il backup nella cartella dati corrente. Verifica percorso e permessi.");
            }
        });

        group.MapGet("/export", async (DataTransferApplicationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var exportDocument = await service.BuildExportAsync(cancellationToken);
                var content = DataTransferApplicationService.SerializeExport(exportDocument);
                var fileName = $"collectify-export-{exportDocument.ExportedAt:yyyyMMddHHmmss}.json";

                return Results.File(content, "application/json", fileName);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return DataTransferProblem(
                    "Export non riuscito",
                    "Collectify non riesce a leggere i file locali necessari per creare l'export.");
            }
        });

        group.MapPost("/import", async (HttpRequest request, DataTransferApplicationService service, CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Seleziona un file JSON esportato da Collectify."]
                });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");

            if (file is null || file.Length == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Il file di import e' obbligatorio."]
                });
            }

            if (!Path.GetExtension(file.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Il file di import deve avere estensione .json."]
                });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await service.ImportAsync(stream, cancellationToken);

                return result.IsValid
                    ? Results.Ok(result.Value)
                    : Results.ValidationProblem(result.Errors);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return DataTransferProblem(
                    "Import non riuscito",
                    "Collectify non riesce a salvare i dati importati nella cartella dati corrente. Verifica percorso e permessi.");
            }
        });

        return endpoints;
    }

    private static IResult DataTransferProblem(string title, string detail)
    {
        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
