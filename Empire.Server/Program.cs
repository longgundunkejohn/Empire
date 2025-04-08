using Empire.Server.Interfaces;
using Empire.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Empire.Shared.Models;
using Empire.Shared.Models.DTOs;
using System.IO;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// ────────────── Services ──────────────

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new MongoClient(config["MongoDB:ConnectionString"]);
});

builder.Services.AddSingleton<DeckService>();
builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ICardDatabaseService, CardGameDatabaseService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddTransient<DeckLoaderService>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://empirecardgame.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ────────────── Build App ──────────────

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use((context, next) =>
{
    context.Request.Host = new HostString("empirecardgame.com");
    return next();
});

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowEmpireClient");
app.MapControllers();
app.MapFallbackToFile("index.html");


// ────────────── Database Seeding ──────────────

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var env = services.GetRequiredService<IHostEnvironment>();
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();

    try
    {
        var logger = services.GetRequiredService<ILogger<DeckLoaderService>>();
        var cardDb = services.GetRequiredService<ICardDatabaseService>();
        var mongo = services.GetRequiredService<IMongoDbService>();

        var deckLoader = new DeckLoaderService(env, mongo, logger, cardDb);
        var deckDb = mongo.DeckDatabase.GetCollection<PlayerDeck>("PlayerDecks");

        string civicDeckPath = Path.Combine(env.ContentRootPath, "wwwroot", "decks", "Player1_Civic.csv");
        string militaryDeckPath = Path.Combine(env.ContentRootPath, "wwwroot", "decks", "Player1_Military.csv");

        if (File.Exists(civicDeckPath) && File.Exists(militaryDeckPath))
        {
            using var civicStream = new FileStream(civicDeckPath, FileMode.Open, FileAccess.Read);
            using var militaryStream = new FileStream(militaryDeckPath, FileMode.Open, FileAccess.Read);

            var civicRaw = deckLoader.ParseDeckFromCsv(civicStream);
            var militaryRaw = deckLoader.ParseDeckFromCsv(militaryStream);

            var combined = civicRaw.Concat(militaryRaw).ToList();
            var playerDeck = deckLoader.ConvertRawDeckToPlayerDeck(combined);

            // Remove any existing deck for this player (optional)
            var filter = Builders<PlayerDeck>.Filter.Empty; // change if you key by player
            await deckDb.DeleteManyAsync(filter);

            await deckDb.InsertOneAsync(playerDeck);
            Console.WriteLine("✔ Seeded Player1 deck into MongoDB.");
        }
        else
        {
            logger.LogError("❌ Deck files not found in wwwroot/decks/");
        }
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "❌ Error occurred while seeding deck.");
    }
}

app.Run();
