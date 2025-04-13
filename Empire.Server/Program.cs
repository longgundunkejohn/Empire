using Empire.Server.Interfaces;
using Empire.Server.Services;
using Empire.Shared.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ────────────── Services ──────────────

// ✅ Mongo setup
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new MongoClient(config["MongoDB:ConnectionString"]);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var config = sp.GetRequiredService<IConfiguration>();
    return client.GetDatabase(config["MongoDB:DatabaseName"]);
});

// ✅ JSON config (camelCase)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// ✅ Core game services
builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ICardDatabaseService, CardGameDatabaseService>();
builder.Services.AddSingleton<ICardService, CardService>(); // CardService holds card definitions in memory

// ✅ Game logic services — thread-safe, shared with per-game state dictionaries
builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DeckService>();
builder.Services.AddSingleton<DeckLoaderService>();
builder.Services.AddSingleton<CardFactory>(); // ✅ Add this to hydrate cards from Atlas

builder.Services.AddMemoryCache(); // Optional caching
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://empirecardgame.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 👈 required for SignalR
    });
});


// ────────────── App Pipeline ──────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// ✅ Handle reverse proxy (e.g. Docker, Nginx)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ✅ Force domain name in prod
app.Use((context, next) =>
{
    context.Request.Host = new HostString("empirecardgame.com");
    return next();
});
app.UseCors("AllowEmpireClient");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowEmpireClient");

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
