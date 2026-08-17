using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
                        EmailConfirmed = true
                    };
                    
                    var result = await userManager.CreateAsync(adminUser, "Test123!");
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                    
                    // Add admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new
            {
                Email = "admin@test.com",
                Password = "Test123!"
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult!.AccessToken;
        }

        [Fact]
        public async Task CreateProductVariant_ReturnsCreated()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product first
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();
            Assert.NotNull(product);

            // Create variant
            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                CostPrice = 50m,
                IsActive = true,
                TrackInventory = true,
                AllowBackorder = false
            });

            Assert.Equal(System.Net.HttpStatusCode.Created, variantResponse.StatusCode);

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();
            Assert.NotNull(variant);
            Assert.Equal("TEST-001-RED", variant.Sku);
            Assert.Equal("Red Variant", variant.Name);
            Assert.Equal(99.99m, variant.Price);
        }

        [Fact]
        public async Task GetProductVariants_ReturnsPagedResults()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();

            // Create variants
            await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                IsActive = true
            });

            await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-001-BLUE",
                Name = "Blue Variant",
                Price = 89.99m,
                IsActive = true
            });

            // Get variants
            var response = await _client.GetAsync($"/api/admin/products/{product.Id}/variants?page=1&pageSize=10");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductVariantDto>>();
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task UpdateProductVariant_ReturnsUpdated()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Create product and variant
            var productResponse = await _client.PostAsJsonAsync("/api/admin/products", new
            {
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();

            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                IsActive = true
            });

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();

            // Update variant
            var updateResponse = await _client.PutAsJsonAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}", new
            {
                Id = variant.Id,
                ProductId = product.Id,
                Sku = "TEST-001-RED-UPD",
                Name = "Red Variant Updated",
                Price = 109.99m,
                IsActive = true
            });

            Assert.Equal(System.Net.HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();
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
                Name = "Test Product",
                Slug = "test-product",
                Sku = "TEST-001",
                BasePrice = 100m,
                Status = "Active",
                IsActive = true
            });

            var product = await productResponse.Content.ReadFromJsonAsync<AdminProductDto>();

            var variantResponse = await _client.PostAsJsonAsync($"/api/admin/products/{product.Id}/variants", new
            {
                ProductId = product.Id,
                Sku = "TEST-001-RED",
                Name = "Red Variant",
                Price = 99.99m,
                IsActive = true
            });

            var variant = await variantResponse.Content.ReadFromJsonAsync<AdminProductVariantDto>();

            // Delete variant
            var deleteResponse = await _client.DeleteAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}");
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify deleted
            var getResponse = await _client.GetAsync($"/api/admin/products/{product.Id}/variants/{variant.Id}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }

    public class AdminProductAttributeControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AdminProductAttributeControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_AdminProductAttribute");
                    });
                    // Add Identity services for RoleManager
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
                        EmailConfirmed = true
                    };
                    
                    var result = await userManager.CreateAsync(adminUser, "Test123!");
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                    
                    // Add admin role
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new
            {
                Email = "admin@test.com",
                Password = "Test123!"
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult!.AccessToken;
        }

        // AdminProductAttributeControllerIntegrationTests GetAdminTokenAsync (updated)
        private async Task<string> GetAdminTokenAsync_Attr()
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
            }

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new
            {
                Email = "admin@test.com",
                Password = "Test123!"
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult!.AccessToken;
        }

        [Fact]
        public async Task CreateProductAttribute_ReturnsCreated()
        {
            var token = await GetAdminTokenAsync_Attr();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/admin/attributes", new
            {
                Name = "Color",
                Code = "color",
                DisplayType = "color",
                IsFilterable = true,
                IsVariant = true,
                IsRequired = true
            });

            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

            var attribute = await response.Content.ReadFromJsonAsync<AdminProductAttributeDto>();
            Assert.NotNull(attribute);
            Assert.Equal("Color", attribute.Name);
            Assert.Equal("color", attribute.Code);
        }

        [Fact]
        public async Task GetProductAttributes_ReturnsPagedResults()
        {
            var token = await GetAdminTokenAsync_Attr();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            await _client.PostAsJsonAsync("/api/admin/attributes", new
            {
                Name = "Color",
                Code = "color",
                DisplayType = "color",
                IsFilterable = true,
                IsVariant = true
            });

            await _client.PostAsJsonAsync("/api/admin/attributes", new
            {
                Name = "Size",
                Code = "size",
                DisplayType = "text",
                IsFilterable = true,
                IsVariant = true
            });

            var response = await _client.GetAsync("/api/admin/attributes?page=1&pageSize=10");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductAttributeDto>>();
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task UpdateProductAttribute_ReturnsUpdated()
        {
            var token = await GetAdminTokenAsync_Attr();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createResponse = await _client.PostAsJsonAsync("/api/admin/attributes", new
            {
                Name = "Color",
                Code = "color",
                DisplayType = "color",
                IsFilterable = true,
                IsVariant = true
            });

            var attribute = await createResponse.Content.ReadFromJsonAsync<AdminProductAttributeDto>();

            var updateResponse = await _client.PutAsJsonAsync($"/api/admin/attributes/{attribute.Id}", new
            {
                Id = attribute.Id,
                Name = "Color Updated",
                Code = "color-updated",
                DisplayType = "color",
                IsFilterable = true,
                IsVariant = true
            });

            Assert.Equal(System.Net.HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProductAttributeDto>();
            Assert.Equal("Color Updated", updated.Name);
            Assert.Equal("color-updated", updated.Code);
        }

        [Fact]
        public async Task DeleteProductAttribute_ReturnsNoContent()
        {
            var token = await GetAdminTokenAsync_Attr();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createResponse = await _client.PostAsJsonAsync("/api/admin/attributes", new
            {
                Name = "Color",
                Code = "color",
                DisplayType = "color",
                IsFilterable = true,
                IsVariant = true
            });

            var attribute = await createResponse.Content.ReadFromJsonAsync<AdminProductAttributeDto>();

            var deleteResponse = await _client.DeleteAsync($"/api/admin/attributes/{attribute.Id}");
            Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }
    }

    // DTOs for testing
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}