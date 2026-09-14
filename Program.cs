using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Trading Cards API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.MapControllers();
app.MapGet("/", () => "Trading Cards API is running!");

app.Run();
