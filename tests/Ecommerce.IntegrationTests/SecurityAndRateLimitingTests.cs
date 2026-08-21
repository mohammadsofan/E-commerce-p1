using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class SecurityHeadersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SecurityHeadersIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_SecurityHeaders");
                    });
                });
            });
        }

        [Fact]
        public async Task Response_Includes_SecurityHeaders()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/products");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
            Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").Single());
        }
    }

    public class RateLimitingIntegrationTests
    {
        [Fact]
        public async Task ExceedingLimit_Returns429()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                // Rate limiting is disabled in the Test environment; use a custom
                // environment with a tiny permit limit to exercise the limiter.
                builder.UseEnvironment("RateLimitTest");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                    {
                        ["RateLimiting:Enabled"] = "true",
                        ["RateLimiting:PermitLimit"] = "2",
                        ["RateLimiting:WindowSeconds"] = "60",
                        ["RateLimiting:QueueLimit"] = "0"
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_RateLimit");
                    });
                });
            });

            var client = factory.CreateClient();

            var first = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            var third = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        }

        [Fact]
        public async Task DisabledInTestEnvironment_No429()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_RateLimitDisabled");
                    });
                });
            });

            var client = factory.CreateClient();

            for (int i = 0; i < 10; i++)
            {
                var response = await client.GetAsync("/api/products");
                Assert.True(response.IsSuccessStatusCode);
            }
        }
    }
}