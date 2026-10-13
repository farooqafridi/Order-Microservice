using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.Data;

var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration["ServiceBus:ConnectionStrings"]
    ?? throw new InvalidOperationException(
        "ServiceBus:ConnectionString is missing.");

builder.Services.AddSingleton(
    new ServiceBusClient(connectionString));

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PaymentDb")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();