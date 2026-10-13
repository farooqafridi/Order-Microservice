using CatalogService.Data;
using CatalogService.Repositories;
using CatalogService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException(
        "Connection string 'CatalogDb' is missing.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<
    IProductRepository,
    ProductRepository>();

//builder.Services.AddSingleton<
//    IProductRepository,
//    InMemoryProductRepository>();


var categoryServiceBaseUrl =
    builder.Configuration["Services:CategoryService:BaseUrl"]
    ?? throw new InvalidOperationException(
        "CategoryService BaseUrl is missing.");

builder.Services.AddHttpClient<ICategoryServiceClient, CategoryServiceClient>(
    client =>
    {
        client.BaseAddress = new Uri(categoryServiceBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
