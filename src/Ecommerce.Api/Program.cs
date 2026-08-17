using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ecommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ecommerce.Api.Middleware;
using Serilog;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

// Configuration & DI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce API",
        Version = "v1",
        Description = "E-Commerce Backend API with Clean Architecture",
        Contact = new OpenApiContact
        {
            Name = "E-Commerce Team",
            Email = "support@ecommerce.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add JWT Bearer auth to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Infrastructure (requires DefaultConnection in config)
builder.Services.AddInfrastructure(builder.Configuration);

// Current user (from JWT claims) for per-user features such as the shopping cart
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Ecommerce.Application.Interfaces.ICurrentUserService, Ecommerce.Api.Services.CurrentUserService>();

// Health Checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>();

        // Prometheus Metrics - only in non-test environments
        if (!builder.Environment.IsEnvironment("Test"))
        {
            builder.Services.AddMetricServer(options =>
            {
                options.Port = 9090;
            });
        }

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

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
        options.AddPolicy("AdminOrCustomer", policy => policy.RequireRole("Admin", "Customer"));
    });
}
catch
{
    // Identity/JWT packages not available; skip runtime configuration. Run `scripts/setup.*` to install packages locally.
}

// Register application handlers if not registered by other DI calls
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();

var app = builder.Build();

// Serilog request logging
app.UseSerilogRequestLogging();

// Seed database on startup (development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Ecommerce.Infrastructure.Persistence.DbSeeder>();
        try
        {
            await seeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Ecommerce.Infrastructure.Persistence.DbSeeder>>();
            logger.LogError(ex, "Database seeding failed");
        }
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Health Checks
app.MapHealthChecks("/health");

// Prometheus Metrics
app.MapMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "E-Commerce API Documentation";
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpMetrics();

app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
