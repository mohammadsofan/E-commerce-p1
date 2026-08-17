using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Identity;
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
    public class AdminProductVariantControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AdminProductVariantControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_AdminProductVariant");
                    });
                    // Add Identity services for RoleManager with Guid key
                    services.AddIdentityCore<Ecommerce.Infrastructure.Identity.ApplicationUser>()
                        .AddRoles<Ecommerce.Infrastructure.Identity.ApplicationRole>()
                        .AddEntityFrameworkStores<ApplicationDbContext>();
                });
            });
            _client = _factory.CreateClient();
        }

        private async Task<string> GetAdminTokenAsync()
        {
            // Create admin user with proper password hash and login
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Ecommerce.Infrastructure.Identity.ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Ecommerce.Infrastructure.Identity.ApplicationRole>>();

                // Ensure Admin role exists
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new Ecommerce.Infrastructure.Identity.ApplicationRole { Name = "Admin" });
                }

                var adminUser = await userManager.FindByEmailAsync("admin@test.com");
                if (adminUser == null)
                {
                    adminUser = new Ecommerce.Infrastructure.Identity.ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = "admin@test.com",
                        Email = "admin@test.com",
                        FirstName = "Admin",
                        LastName = "User",
                        EmailConfirmed = true,
                        IsEmailVerified = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Test123!");
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }

                    // Add admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new
                {
                    Email = "admin@test.com",
                    Password = "Test123!"
                });

                var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
                return loginResult!.Token;
            }
        }

        [Fact]
        public async Task CreateProductVariant_ReturnsCreated()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product first
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product Create",
                Slug = "test-product-create",
                Sku = "TEST-CREATE-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();
            Assert.NotNull(product);

            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-CREATE-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                CostPrice = 50m,
                IsActive = true,
                TrackInventory = true,
                AllowBackorder = false
            });

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();
            Assert.NotNull(variant);
            Assert.Equal("Red Variant", variant.Name);
            Assert.Equal(product.Id, variant.ProductId);
        }

        [Fact]
        public async Task GetProductVariants_ReturnsPagedResults()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product first
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product Get",
                Slug = "test-product-get",
                Sku = "TEST-GET-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();
            Assert.NotNull(product);

            // Create variants
            await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-GET-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                IsActive = true
            });

            await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-GET-001-BLUE",
                Name = "Blue Variant",
                Price = 89.99m,
                IsActive = true
            });

            var response = await _client.GetAsync($"/api/admin/products/{product.Id}/variants?page=1&pageSize=10");
            var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductVariantDto>>();

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task UpdateProductVariant_ReturnsUpdated()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product and variant
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product Update",
                Slug = "test-product-update",
                Sku = "TEST-UPDATE-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();

            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-UPDATE-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                CostPrice = 50m,
                IsActive = true,
                TrackInventory = true,
                AllowBackorder = false
            });

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();

            var updateResponse = await _client.PutAsJsonAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}", new
            {
                Id = variant.Id,
                ProductId = product.Id,
                Sku = "TEST-UPDATE-001-RED-UPD",
                Name = "Red Variant Updated",
                Price = 109.99m,
                IsActive = true
            });

            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();
            Assert.NotNull(updated);
            Assert.Equal("Red Variant Updated", updated.Name);
            Assert.Equal(109.99m, updated.Price);
        }

        [Fact]
        public async Task DeleteProductVariant_ReturnsNoContent()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product and variant
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product Delete",
                Slug = "test-product-delete",
                Sku = "TEST-DELETE-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();

            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-DELETE-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                IsActive = true
            });

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();

            var deleteResponse = await _client.DeleteAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}");
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var getResponse = await _client.GetAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}