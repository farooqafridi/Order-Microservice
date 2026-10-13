using CartService.Data;
using CartService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("CartDb")
    ?? throw new InvalidOperationException(
        "Connection string 'CartDb' is missing.");

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseSqlServer(connectionString));

var catalogBaseUrl =
    builder.Configuration["Services:CatalogService:BaseUrl"]
    ?? throw new InvalidOperationException(
        "CatalogService BaseUrl is missing.");

builder.Services.AddHttpClient<
    ICatalogServiceClient, CatalogServiceClient>(client =>
    {
        client.BaseAddress = new Uri(catalogBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });


var jwtSection = builder.Configuration.GetSection("Jwt");

var issuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

var audience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

var signingKey = builder.Configuration["Jwt:SigningKey"];

if (string.IsNullOrWhiteSpace(signingKey) ||
    Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
