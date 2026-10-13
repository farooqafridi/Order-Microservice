using Azure.Messaging.ServiceBus;
using InventoryService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Workers;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

//builder.Services.AddGrpcClient<Inventory.InventoryClient>(options =>
//{
//    options.Address = new Uri(
//        builder.Configuration["InventoryService:Url"]!);
//});


builder.Services.AddGrpcClient<Inventory.InventoryClient>(options =>
{
    options.Address = new Uri("http://inventoryservice:8080");
});

var connectionString =
    builder.Configuration["ServiceBus:ConnectionStrings"]
    ?? throw new InvalidOperationException(
        "ServiceBus:ConnectionString is missing.");

var queueName =
    builder.Configuration["ServiceBus:OrdersQueue"]
    ?? throw new InvalidOperationException(
        "ServiceBus:QueueName is missing.");

builder.Services.AddSingleton(_ =>
    new ServiceBusClient(connectionString));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<ServiceBusClient>()
        .CreateSender(queueName));

builder.Services.AddScoped<
    IOrderEventPublisher,
    ServiceBusOrderEventPublisher>();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrderDb")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddHttpClient<ICartServiceClient, CartServiceClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["CartService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "CartService BaseUrl is missing."));
    });

builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(
    client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["CatalogService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "CatalogService BaseUrl is missing."));
    });

var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");
var audience = jwt["Audience"]
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHostedService<PaymentResultWorker>();

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
