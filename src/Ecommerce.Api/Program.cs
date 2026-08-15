using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ecommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configuration & DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure (requires DefaultConnection in config)
builder.Services.AddInfrastructure(builder.Configuration);

// Register application handlers if not registered by other DI calls
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
