using Empire.Server.Interfaces;
using Empire.Server.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ───── Services ─────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// 🗄 MongoDB setup
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>()
        .GetDatabase(builder.Configuration["MongoDB:DatabaseName"]));

// 💼 Application services
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddSingleton<DeckLoaderService>();
builder.Services.AddScoped<ICardDatabaseService, CardDatabaseService>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<CardFactory>();
builder.Services.AddScoped<GameSessionService>();

// 🌐 CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5049",
            "http://localhost:5049",
            "http://138.68.188.47",
            "https://empirecardgame.com",
            "https://www.empirecardgame.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// ───── App Pipeline ─────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // (Optional) app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts(); // Production security
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowEmpireClient");
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
