using Collectify.Api.DevTools;
using Collectify.Api.Modules.Collections;
using Collectify.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<ICollectifyDataStore, JsonCollectifyDataStore>();
builder.Services.AddSingleton<ICollectionRepository, JsonCollectionRepository>();
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

app.Run();

public partial class Program;
