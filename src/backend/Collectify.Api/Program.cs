using Collectify.Api.DevTools;
using Collectify.Api.Modules.Collections;
using Collectify.Api.Modules.ExternalMetadata;
using Collectify.Api.Modules.Search;
using Collectify.Api.Modules.Settings;
using Collectify.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CollectifyDesktop", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<LocalDataOptions>(builder.Configuration.GetSection("LocalData"));
builder.Services.Configure<ExternalMetadataOptions>(builder.Configuration.GetSection("ExternalMetadata"));
builder.Services.AddHttpClient("ExternalMetadata", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Collectify/0.1");
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSingleton<LocalDataPathResolver>();
builder.Services.AddSingleton<AppSettingsFileStore>();
builder.Services.AddSingleton<AppSettingsApplicationService>();
builder.Services.AddSingleton<ICollectifyDataStore, JsonCollectifyDataStore>();
builder.Services.AddSingleton<ICollectionRepository, JsonCollectionRepository>();
builder.Services.AddSingleton<CollectionApplicationService>();
builder.Services.AddSingleton<ItemImageApplicationService>();
builder.Services.AddSingleton<IExternalMetadataProvider, TmdbMetadataProvider>();
builder.Services.AddSingleton<IExternalMetadataProvider, RawgMetadataProvider>();
builder.Services.AddSingleton<IExternalMetadataProvider, DiscogsMetadataProvider>();
builder.Services.AddSingleton<ExternalMetadataApplicationService>();
builder.Services.AddSingleton<LocalSearchApplicationService>();
builder.Services.AddSingleton<IUserProfileRepository, JsonUserProfileRepository>();
builder.Services.AddSingleton<ICollectionCategoryRepository, JsonCollectionCategoryRepository>();
builder.Services.AddSingleton<ITagRepository, JsonTagRepository>();
builder.Services.AddSingleton<IAppSettingsRepository, JsonAppSettingsRepository>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<FrontendLauncher>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("CollectifyDesktop");

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "Collectify.Api",
    storage = "JsonFile",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapCollectionEndpoints();
app.MapAssetEndpoints();
app.MapExternalMetadataEndpoints();
app.MapSearchEndpoints();
app.MapSettingsEndpoints();

app.Run();

public partial class Program;
