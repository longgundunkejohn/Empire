using Empire.Server.Interfaces;
using Empire.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using MongoDB.Driver;
using Microsoft.Extensions.Hosting;
using Empire.Shared.Models;
using Microsoft.AspNetCore.Builder; // Add this

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["MongoDB:ConnectionString"];
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<ICardDatabaseService, CardGameDatabaseService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddTransient<DeckLoaderService>();
builder.Services.AddControllers(); // THIS LINE IS REQUIRED!
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmpireClient",
        builder =>
        {
            builder.WithOrigins("http://localhost:5173", "https://empirecardgame.com") // Allow your client's origin
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build(); // Add this back - VERY IMPORTANT!

// ───── App Pipeline ─────

if (app.Environment.IsDevelopment()) // Use the 'app' variable
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

// 💾 Database Seeding (Example)
using (var scope = app.Services.CreateScope()) // Use the 'app' variable
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        var deckLoader = services.GetRequiredService<DeckLoaderService>();
        var mongoClient = services.GetRequiredService<IMongoClient>();
        var databaseName = builder.Configuration["MongoDB:DatabaseName"];
        var database = mongoClient.GetDatabase(databaseName);
        var deckCollection = database.GetCollection<List<Card>>("Decks"); // Or a more specific name
        var cardDatabaseService = services.GetRequiredService<ICardDatabaseService>();
        var hostEnvironment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<DeckLoaderService>>();

        string civicDeckPath = "wwwroot/decks/Player1_Civic.csv";
        string militaryDeckPath = "wwwroot/decks/Player1_Military.csv";

        using var civicStream = new FileStream(civicDeckPath, FileMode.Open, FileAccess.Read);
        using var militaryStream = new FileStream(militaryDeckPath, FileMode.Open, FileAccess.Read);

        //Recreate deckLoader with correct dependencies
        deckLoader = new DeckLoaderService(hostEnvironment, logger, cardDatabaseService);

        var civicDeck = deckLoader.ParseDeckFromCsv(civicStream);
        civicStream.Position = 0;
        var militaryDeck = deckLoader.ParseDeckFromCsv(militaryStream);

        var fullDeck = new List<Card>();
        fullDeck.AddRange(civicDeck);
        fullDeck.AddRange(militaryDeck);

        await deckCollection.InsertOneAsync(fullDeck);
        Console.WriteLine("✔ Deck data seeded into MongoDB.");
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred seeding the database.");
    }
}

app.Run();