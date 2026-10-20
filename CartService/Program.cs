using CartService.Data;
using CartService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Polly;
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


builder.WebHost.ConfigureKestrel(options =>
{
    // 1. Endpoints
    options.ListenAnyIP(5000);

    // 2. Request limits
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;

    // 3. Timeouts
    options.Limits.RequestHeadersTimeout =
        TimeSpan.FromSeconds(30);

    options.Limits.KeepAliveTimeout =
        TimeSpan.FromMinutes(2);

    // 4. Connection limit
    options.Limits.MaxConcurrentConnections = 1000;
});

builder.Services.AddHttpClient<
    ICatalogServiceClient, CatalogServiceClient>(client =>
    {
        client.BaseAddress = new Uri(catalogBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    }).AddStandardResilienceHandler(options =>
    {
        // 1. Limit concurrent outbound requests
        options.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 10;
        options.RateLimiter.DefaultRateLimiterOptions.QueueLimit = 0;

        // 2. Overall request timeout
        options.TotalRequestTimeout.Timeout =
            TimeSpan.FromSeconds(8);

        // 3. Retry transient failures
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.DisableForUnsafeHttpMethods();

        // 4. Circuit breaker
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration =
            TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration =
            TimeSpan.FromSeconds(10);

        // 5. Timeout per attempt
        options.AttemptTimeout.Timeout =
            TimeSpan.FromSeconds(3);
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

public partial class Program;