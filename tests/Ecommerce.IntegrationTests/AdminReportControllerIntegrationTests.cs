using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class AdminReportControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AdminReportControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_AdminReports");
                    });
                    services.AddIdentityCore<ApplicationUser>()
                        .AddRoles<ApplicationRole>()
                        .AddEntityFrameworkStores<ApplicationDbContext>();
                });
            });
            _client = _factory.CreateClient();
        }

        private async Task<string> GetAdminTokenAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<ApplicationRole>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
            }

            var adminUser = await userManager.FindByEmailAsync("admin.reports@test.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "admin.reports@test.com",
                    Email = "admin.reports@test.com",
                    FirstName = "Admin",
                    LastName = "Reports",
                    EmailConfirmed = true,
                    IsEmailVerified = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Test123!");
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                adminUser.IsActive = true;
                adminUser.IsEmailVerified = true;
                await userManager.UpdateAsync(adminUser);
            }

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new
            {
                Email = "admin.reports@test.com",
                Password = "Test123!"
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult!.Token;
        }

        [Fact]
        public async Task GetSalesReport_ReturnsOk_WithData()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/admin/reports/sales?groupBy=day");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<SalesReportDto>();
            Assert.NotNull(report);
            Assert.NotNull(report.SalesByPeriod);
            Assert.NotNull(report.TopProducts);
            Assert.NotNull(report.TopCategories);
        }

        [Fact]
        public async Task GetRevenueReport_ReturnsOk_WithData()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/admin/reports/revenue?groupBy=week");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<RevenueReportDto>();
            Assert.NotNull(report);
            Assert.NotNull(report.RevenueByPeriod);
            Assert.NotNull(report.RevenueByChannel);
        }

        [Fact]
        public async Task GetInventoryReport_ReturnsOk_WithData()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/admin/reports/inventory");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<InventoryReportDto>();
            Assert.NotNull(report);
            Assert.NotNull(report.ByWarehouse);
            Assert.NotNull(report.ByCategory);
        }

        [Fact]
        public async Task GetCustomerReport_ReturnsOk_WithData()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/admin/reports/customers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var report = await response.Content.ReadFromJsonAsync<CustomerReportDto>();
            Assert.NotNull(report);
            Assert.NotNull(report.Segments);
        }

        [Fact]
        public async Task ExportReport_ReturnsFileDownload()
        {
            var token = await GetAdminTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("/api/admin/reports/export", new
            {
                ReportType = "sales",
                Format = "csv",
                GroupBy = "day"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task GetSalesReport_WithoutAuth_ReturnsUnauthorized()
        {
            var unauthClient = _factory.CreateClient();
            var response = await unauthClient.GetAsync("/api/admin/reports/sales");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

