using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ecommerce.Api.Middleware;
using Serilog;
using Prometheus;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:5173", "https://localhost:3000", "https://localhost:5173" };

    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddEndpointsApiExplorer();

// Rate limiting (ASP.NET Core built-in). Configurable via "RateLimiting" section.
builder.Services.AddRateLimiter(options =>
{
    var rlSection = builder.Configuration.GetSection("RateLimiting");
    var permitLimit = rlSection.GetValue<int>("PermitLimit", 100);
    var windowSeconds = rlSection.GetValue<int>("WindowSeconds", 60);
    var queueLimit = rlSection.GetValue<int>("QueueLimit", 0);
    var enabled = rlSection.GetValue<bool>("Enabled", true) && !builder.Environment.IsEnvironment("Test");

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = windowSeconds.ToString();
        await context.HttpContext.Response.WriteAsync("{\"error\":\"Too many requests. Please try again later.\"}", cancellationToken);
    };

    if (enabled)
    {
        // Global per-client (IP) fixed-window throttle.
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(
            context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = queueLimit,
                    AutoReplenishment = true
                }));
    }
    else
    {
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(
            _ => System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("all"));
    }
});

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

// Prometheus Metrics standalone server - only in non-test environments.
// Skipped for any environment whose name contains "Test" (e.g. "Test",
// "RateLimitTest") so test hosts don't collide on port 9090.
        // DISABLED to avoid port 9090 conflicts
        // if (!builder.Environment.EnvironmentName.Contains("Test", StringComparison.OrdinalIgnoreCase))
        // {
        //     builder.Services.AddMetricServer(options =>
        //     {
        //         options.Port = 9090;
        //     });
        // }

// OpenTelemetry tracing - enabled via "Tracing:Enabled" and skipped in Test environment.
var tracingEnabled = builder.Configuration.GetValue<bool>("Tracing:Enabled", false) && !builder.Environment.IsEnvironment("Test");
if (tracingEnabled)
{
    var otlpEndpoint = builder.Configuration["Tracing:OtlpEndpoint"] ?? "http://localhost:4317";
    var serviceName = builder.Configuration["Tracing:ServiceName"] ?? "Ecommerce.Api";

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        });
}


// Configure Identity and JWT authentication
builder.Services.AddIdentity<Ecommerce.Infrastructure.Identity.ApplicationUser, Ecommerce.Infrastructure.Identity.ApplicationRole>()
    .AddEntityFrameworkStores<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>()
    .AddDefaultTokenProviders();

var key = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"] ?? "ecommerce";

if (string.IsNullOrWhiteSpace(key) || key == "change_this_dev_secret_to_a_long_random_value" || System.Text.Encoding.UTF8.GetByteCount(key) < 32)
{
    throw new InvalidOperationException("Jwt:Key is not configured properly. A secure key of at least 256 bits (32 bytes) is required.");
}

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

// Register application handlers if not registered by other DI calls
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommand, Ecommerce.Application.Common.Unit>, Ecommerce.Application.Commands.ReserveInventory.ReserveInventoryCommandHandler>();
builder.Services.AddScoped<Ecommerce.Application.Common.Commands.ICommandHandler<Ecommerce.Application.Commands.Checkout.CheckoutCommand, System.Guid>, Ecommerce.Application.Commands.Checkout.CheckoutCommandHandler>();

var app = builder.Build();

// Correlation ID propagation (must run early so logs/metrics/traces share it)
app.UseMiddleware<CorrelationIdMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging();

// Seed database on startup (development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Ecommerce.Infrastructure.Persistence.ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Ecommerce.Infrastructure.Persistence.DbSeeder>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        try
        {
            await seeder.SeedAsync(db, roleManager);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Ecommerce.Infrastructure.Persistence.DbSeeder>>();
            logger.LogError(ex, "Database seeding failed");
        }
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Rate limiting middleware (after routing so policies can be attached per-endpoint)
app.UseRateLimiter();

// HTTPS enforcement
if (!app.Environment.IsEnvironment("Test") && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    // HSTS - only in non-development environments
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
}

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
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// app.UseHttpMetrics();

app.MapControllers();

// Seed initial database data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
        await seeder.SeedAsync(db, roleManager);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database on startup.");
    }
}

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
