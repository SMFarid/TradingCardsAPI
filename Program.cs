using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TradingCardsAPI.Data;
using TradingCardsAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Hosting platforms (Render, Railway, Fly, ...) inject the port to listen on.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// Card identification via external TCG APIs (Scryfall requires a User-Agent header).
builder.Services.AddHttpClient<CardLookupService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CardVault/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                      });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(ResolveConnectionString(builder.Configuration)));

var app = builder.Build();

// Managed databases start empty, so bring the schema up to date on boot.
// Failing fast here surfaces a misconfigured database in the deploy logs
// instead of as mysterious 500s on every request.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database unavailable or migrations failed. "
            + "Check the DATABASE_URL / ConnectionStrings__DefaultConnection setting.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Trading Cards API v1");
    });
}

// HTTPS is terminated by the hosting platform in production; redirecting
// here would fight the reverse proxy (and dev runs plain HTTP anyway).
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "Trading Cards API is running!");

// Lets a deployment be checked without digging through platform logs.
// Reports whether the database is reachable without revealing its details.
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { status = canConnect ? "ok" : "degraded", database = canConnect });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "error", database = false, reason = ex.GetType().Name });
    }
});

app.Run();

/// <summary>
/// Managed Postgres providers (Render, Railway, Heroku) expose a URL-style
/// connection string, which Npgsql cannot parse; convert it to keyword form.
/// Falls back to the configured connection string for local development.
/// </summary>
static string? ResolveConnectionString(IConfiguration config)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(databaseUrl) || !databaseUrl.Contains("://"))
        return config.GetConnectionString("DefaultConnection");

    var uri = new Uri(databaseUrl);
    var credentials = uri.UserInfo.Split(':', 2);

    var connection = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
        // Managed instances require TLS but present certificates this app has
        // no chain for, so encrypt without validating.
        SslMode = Npgsql.SslMode.Require,
        TrustServerCertificate = true,
    };

    return connection.ConnectionString;
}
