using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TradingCardsAPI.Data;
using TradingCardsAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "Trading Cards API is running!");

app.Run();
