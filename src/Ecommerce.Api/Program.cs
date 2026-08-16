using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ecommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configuration & DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure (requires DefaultConnection in config)
builder.Services.AddInfrastructure(builder.Configuration);

// Current user (from JWT claims) for per-user features such as the shopping cart
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Ecommerce.Application.Interfaces.ICurrentUserService, Ecommerce.Api.Services.CurrentUserService>();

// Configure Identity and JWT authentication (best-effort — requires Identity & JWT packages locally)
try
{
    builder.Services.AddIdentity<Ecommerce.Infrastructure.Identity.ApplicationUser, Ecommerce.Infrastructure.Identity.ApplicationRole>()
        .AddEntityFrameworkStores<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>()
        .AddDefaultTokenProviders();

    var key = builder.Configuration["Jwt:Key"] ?? "change_this_dev_secret_to_a_long_random_value";
    var issuer = builder.Configuration["Jwt:Issuer"] ?? "ecommerce";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = issuer,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key))
        };
    });

    builder.Services.AddAuthorization();
}
catch
{
    // Identity/JWT packages not available; skip runtime configuration. Run `scripts/setup.*` to install packages locally.
}

// Register application handlers if not registered by other DI calls
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
