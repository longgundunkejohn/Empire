using Empire.Server.Hubs;
using Empire.Server.Middleware;
using Empire.Server.Services;
using Empire.Server.Data;
using Empire.Shared.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ────────────── Services ──────────────

// ✅ SQLite Database
builder.Services.AddDbContext<EmpireDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=empire.db"));

// ✅ JSON & File Upload Configuration
builder.Services.AddControllers().AddJsonOptions(opts =>
    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// ✅ Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// ✅ Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "EmpireTCG",
            ValidAudience = jwtSettings["Audience"] ?? "EmpireTCGUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ✅ Game services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddScoped<ICardDatabaseService, JsonCardDataService>();
builder.Services.AddSingleton<ICardService, CardService>();
builder.Services.AddSingleton<GameSessionService>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<DeckService>();
builder.Services.AddSingleton<DeckLoaderService>();
builder.Services.AddSingleton<CardFactory>();
builder.Services.AddScoped<CardEffectService>();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen();

// ✅ SignalR
builder.Services.AddSignalR().AddJsonProtocol(opt =>
    opt.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// ✅ CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowEmpireClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://empirecardgame.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ────────────── Pipeline ──────────────
var app = builder.Build();

// ✅ Initialize Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
    context.Database.EnsureCreated();
}

// ✅ Error handling middleware (must be first)
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// ✅ Docker/Nginx reverse proxy headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ✅ Force domain (prod) - Only in production
if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Host = new HostString("empirecardgame.com");
        return next();
    });
}

app.UseCors("AllowEmpireClient");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/gamehub");
app.MapFallbackToFile("index.html");

app.Run();
