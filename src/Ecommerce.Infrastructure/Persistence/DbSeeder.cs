using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Persistence
{
    public class DbSeeder
    {
        private readonly ILogger<DbSeeder> _logger;

        public DbSeeder(ILogger<DbSeeder> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(ApplicationDbContext db, RoleManager<ApplicationRole>? roleManager = null)
        {
            try
            {
                // Ensure database is created
                await db.Database.EnsureCreatedAsync();

                // Seed roles
                await SeedRolesAsync(roleManager);

                // Seed currencies
                await SeedCurrenciesAsync(db);

                // Seed categories
                await SeedCategoriesAsync(db);

                // Seed brands
                await SeedBrandsAsync(db);

                // Seed warehouses
                await SeedWarehousesAsync(db);

                // Seed tax categories
                await SeedTaxCategoriesAsync(db);

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedCurrenciesAsync(ApplicationDbContext db)
        {
            if (await db.Currencies.AnyAsync()) return;

            var currencies = new List<Currency>
            {
                new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", IsBaseCurrency = true },
                new Currency { Id = Guid.NewGuid(), Code = "EUR", Symbol = "€", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "GBP", Symbol = "£", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "CAD", Symbol = "C$", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "AUD", Symbol = "A$", IsBaseCurrency = false },
            };

            await db.Currencies.AddRangeAsync(currencies);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} currencies", currencies.Count);
        }

        private async Task SeedCategoriesAsync(ApplicationDbContext db)
        {
            if (await db.Categories.AnyAsync()) return;

            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics", Description = "Electronic devices and accessories", ImageUrl = "", DisplayOrder = 1, IsActive = true, IsFeatured = true, MetaTitle = "Electronics", MetaDescription = "Shop electronics", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Category { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing", Description = "Apparel and fashion items", ImageUrl = "", DisplayOrder = 2, IsActive = true, IsFeatured = true, MetaTitle = "Clothing", MetaDescription = "Shop clothing", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Category { Id = Guid.NewGuid(), Name = "Home & Garden", Slug = "home-garden", Description = "Home improvement and garden supplies", ImageUrl = "", DisplayOrder = 3, IsActive = true, IsFeatured = false, MetaTitle = "Home & Garden", MetaDescription = "Shop home & garden", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Category { Id = Guid.NewGuid(), Name = "Sports & Outdoors", Slug = "sports-outdoors", Description = "Sports equipment and outdoor gear", ImageUrl = "", DisplayOrder = 4, IsActive = true, IsFeatured = false, MetaTitle = "Sports & Outdoors", MetaDescription = "Shop sports", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Category { Id = Guid.NewGuid(), Name = "Books", Slug = "books", Description = "Books and publications", ImageUrl = "", DisplayOrder = 5, IsActive = true, IsFeatured = false, MetaTitle = "Books", MetaDescription = "Shop books", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
            };

            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} categories", categories.Count);
        }

        private async Task SeedBrandsAsync(ApplicationDbContext db)
        {
            if (await db.Brands.AnyAsync()) return;

            var brands = new List<Brand>
            {
                new Brand { Id = Guid.NewGuid(), Name = "TechBrand", Slug = "techbrand", Description = "Leading technology brand", ImageUrl = "", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Brand { Id = Guid.NewGuid(), Name = "FashionCo", Slug = "fashionco", Description = "Premium fashion brand", ImageUrl = "", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
                new Brand { Id = Guid.NewGuid(), Name = "HomeEssentials", Slug = "homeessentials", Description = "Quality home products", ImageUrl = "", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, IsDeleted = false },
            };

            await db.Brands.AddRangeAsync(brands);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} brands", brands.Count);
        }

        private async Task SeedWarehousesAsync(ApplicationDbContext db)
        {
            if (await db.Warehouses.AnyAsync()) return;

            var warehouses = new List<Warehouse>
            {
                new Warehouse { Id = Guid.NewGuid(), Name = "Main Warehouse", Code = "WH-MAIN", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Warehouse { Id = Guid.NewGuid(), Name = "East Coast Warehouse", Code = "WH-EAST", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Warehouse { Id = Guid.NewGuid(), Name = "West Coast Warehouse", Code = "WH-WEST", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            };

            await db.Warehouses.AddRangeAsync(warehouses);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} warehouses", warehouses.Count);
        }

        private async Task SeedTaxCategoriesAsync(ApplicationDbContext db)
        {
            if (await db.TaxCategories.AnyAsync()) return;

            var taxCategories = new List<TaxCategory>
            {
                new TaxCategory { Id = Guid.NewGuid(), Name = "Standard", Description = "Standard tax rate", CreatedAt = DateTimeOffset.UtcNow },
                new TaxCategory { Id = Guid.NewGuid(), Name = "Reduced", Description = "Reduced tax rate for essentials", CreatedAt = DateTimeOffset.UtcNow },
                new TaxCategory { Id = Guid.NewGuid(), Name = "Zero", Description = "Zero-rated items", CreatedAt = DateTimeOffset.UtcNow },
            };

            await db.TaxCategories.AddRangeAsync(taxCategories);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} tax categories", taxCategories.Count);
        }

        private async Task SeedRolesAsync(RoleManager<ApplicationRole>? roleManager)
        {
            if (roleManager == null) return;

            string[] roles = { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role, Description = $"{role} role", CreatedAt = DateTimeOffset.UtcNow });
                    _logger.LogInformation("Seeded role: {Role}", role);
                }
            }
        }
    }
}