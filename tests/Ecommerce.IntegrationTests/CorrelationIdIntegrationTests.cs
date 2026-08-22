using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Ecommerce.IntegrationTests
{
    public class CorrelationIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public CorrelationIdIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_Correlation");
                    });
                });
            });
        }

        [Fact]
        public async Task Response_Includes_GeneratedCorrelationId()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/products");

            Assert.True(response.IsSuccessStatusCode);
            var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();
            Assert.False(string.IsNullOrWhiteSpace(correlationId));
            Assert.Equal(32, correlationId.Length); // generated Guid (no dashes)
        }

        [Fact]
        public async Task Response_Echoes_IncomingCorrelationId()
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", "my-custom-trace-id-123");

            var response = await client.SendAsync(request);

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("my-custom-trace-id-123", response.Headers.GetValues("X-Correlation-Id").Single());
        }

        [Fact]
        public async Task TwoRequests_Get_DifferentGeneratedCorrelationIds()
        {
            var client = _factory.CreateClient();

            var first = await client.GetAsync("/api/products");
            var second = await client.GetAsync("/api/products");

            var firstId = first.Headers.GetValues("X-Correlation-Id").Single();
            var secondId = second.Headers.GetValues("X-Correlation-Id").Single();
            Assert.NotEqual(firstId, secondId);
        }

        [Fact]
        public async Task ErrorResponse_StillHas_CorrelationId()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/api/products/{System.Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();
            Assert.False(string.IsNullOrWhiteSpace(correlationId));
        }
    }
}

