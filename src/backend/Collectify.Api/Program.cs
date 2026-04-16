using Collectify.Api.DevTools;
using Collectify.Api.Modules.Collections;

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

builder.Services.AddSingleton<ICollectionRepository, InMemoryCollectionRepository>();

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
    storage = "InMemory",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapCollectionEndpoints();

app.Run();

public partial class Program;
