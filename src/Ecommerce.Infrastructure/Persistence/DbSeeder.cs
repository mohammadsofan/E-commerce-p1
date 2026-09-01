using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Identity;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Infrastructure.Persistence
{
    public class DbSeeder
    {
        private readonly ILogger<DbSeeder> _logger;
        private readonly IConfiguration _configuration;
        private readonly Ecommerce.Application.Interfaces.IProductSearchService _searchService;

        public DbSeeder(
            ILogger<DbSeeder> logger,
            IConfiguration configuration,
            Ecommerce.Application.Interfaces.IProductSearchService searchService)
        {
            _logger = logger;
            _configuration = configuration;
            _searchService = searchService;
        }

        public async Task SeedAsync(ApplicationDbContext db, RoleManager<ApplicationRole>? roleManager = null, UserManager<ApplicationUser>? userManager = null)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();

                await SeedRolesAsync(roleManager);
                await SeedUsersAsync(db, userManager);
                await SeedCurrenciesAsync(db);
                await SeedWarehousesAsync(db);
                await SeedShippingZonesAsync(db);
                await SeedHeroBannersAsync(db);
                await SeedStoreFeaturesAsync(db);
                await SeedTagsAsync(db);
                await SeedCategoriesAsync(db);
                await SeedBrandsAsync(db);
                await SeedProductsAsync(db);
                await SeedProductReviewsAsync(db, userManager);
                await SeedPromotionsAsync(db);
                await SeedCouponsAsync(db);
                await SeedSampleOrdersAsync(db);

                _logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
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

        private async Task SeedUsersAsync(ApplicationDbContext db, UserManager<ApplicationUser>? userManager)
        {
            if (userManager == null) return;

            var testEmail = "e2e-customer@example.com";
            var existingUser = await userManager.FindByEmailAsync(testEmail);
            ApplicationUser? user = existingUser;
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = testEmail,
                    Email = testEmail,
                    FirstName = "عميل",
                    LastName = "تجريبي",
                    DisplayName = "عميل تجريبي",
                    EmailConfirmed = true,
                    IsEmailVerified = true,
                    IsActive = true,
                    PhoneNumber = "0599123456",
                    PhoneNumberConfirmed = true,
                    IsPhoneVerified = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var result = await userManager.CreateAsync(user, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Customer");
                    _logger.LogInformation("Seeded E2E test user: {Email}", testEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to seed E2E test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!user.EmailConfirmed || !user.IsEmailVerified || !user.IsActive)
                {
                    user.EmailConfirmed = true;
                    user.IsEmailVerified = true;
                    user.IsActive = true;
                    await userManager.UpdateAsync(user);
                }
            }

            // Seed default Palestinian Address for the test user if none exists
            if (user != null && !await db.Addresses.AnyAsync(a => a.UserId == user.Id && !a.IsDeleted))
            {
                var address = new Address
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Type = "Shipping",
                    FirstName = "عميل",
                    LastName = "تجريبي",
                    AddressLine1 = "شارع الإرسال، عمارة البرج، طابق 4",
                    City = "رام الله",
                    State = "الضفة الغربية",
                    PostalCode = "00970",
                    CountryCode = "PS",
                    PhoneNumber = "0599123456",
                    IsDefaultShipping = true,
                    IsDefaultBilling = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await db.Addresses.AddAsync(address);
                await db.SaveChangesAsync();
                _logger.LogInformation("Seeded default shipping address for E2E user.");
            }
        }

        private async Task SeedCurrenciesAsync(ApplicationDbContext db)
        {
            var existing = await db.Currencies.ToListAsync();
            if (!existing.Any())
            {
                var currencies = new List<Currency>
                {
                    new Currency { Id = Guid.NewGuid(), Code = "ILS", Symbol = "₪", IsBaseCurrency = true },
                    new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", IsBaseCurrency = false },
                    new Currency { Id = Guid.NewGuid(), Code = "EUR", Symbol = "€", IsBaseCurrency = false },
                    new Currency { Id = Guid.NewGuid(), Code = "GBP", Symbol = "£", IsBaseCurrency = false },
                    new Currency { Id = Guid.NewGuid(), Code = "JOD", Symbol = "د.أ", IsBaseCurrency = false },
                    new Currency { Id = Guid.NewGuid(), Code = "SAR", Symbol = "ر.س", IsBaseCurrency = false },
                    new Currency { Id = Guid.NewGuid(), Code = "AED", Symbol = "د.إ", IsBaseCurrency = false },
                };
                await db.Currencies.AddRangeAsync(currencies);
                await db.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} currencies with ILS as base", currencies.Count);
                existing = currencies;
            }

            var baseCurrency = existing.FirstOrDefault(c => c.IsBaseCurrency);
            var ils = existing.FirstOrDefault(c => c.Code == "ILS");
            if (baseCurrency == null || baseCurrency.Code != "ILS")
            {
                foreach (var c in existing) c.IsBaseCurrency = false;
                if (ils != null) ils.IsBaseCurrency = true;
                else
                {
                    ils = new Currency { Id = Guid.NewGuid(), Code = "ILS", Symbol = "₪", IsBaseCurrency = true };
                    await db.Currencies.AddAsync(ils);
                    existing.Add(ils);
                }
                await db.SaveChangesAsync();
                _logger.LogInformation("Repaired base currency to ILS");
            }

            var expected = new Dictionary<string, string>
            {
                ["USD"] = "$", ["EUR"] = "€", ["GBP"] = "£", ["JOD"] = "د.أ", ["SAR"] = "ر.س", ["AED"] = "د.إ"
            };
            var added = 0;
            foreach (var kv in expected)
            {
                if (!existing.Any(c => c.Code == kv.Key))
                {
                    await db.Currencies.AddAsync(new Currency { Id = Guid.NewGuid(), Code = kv.Key, Symbol = kv.Value, IsBaseCurrency = false });
                    added++;
                }
            }
            if (added > 0) await db.SaveChangesAsync();
            if (added > 0) _logger.LogInformation("Seeded {Count} missing currencies", added);

            await SeedExchangeRatesAsync(db);
        }

        private async Task SeedExchangeRatesAsync(ApplicationDbContext db)
        {
            var currencies = await db.Currencies.ToListAsync();
            var ils = currencies.FirstOrDefault(c => c.Code == "ILS");
            if (ils == null) return;

            var seededRates = new Dictionary<string, decimal>
            {
                ["USD"] = 0.27m,
                ["EUR"] = 0.25m,
                ["GBP"] = 0.22m,
                ["JOD"] = 0.19m,
                ["SAR"] = 1.02m,
                ["AED"] = 0.99m,
            };
            var now = DateTimeOffset.UtcNow;
            var created = 0;
            foreach (var kv in seededRates)
            {
                var target = currencies.FirstOrDefault(c => c.Code == kv.Key);
                if (target == null) continue;
                var alreadyExists = await db.ExchangeRates.AnyAsync(r => r.FromCurrencyId == ils.Id && r.ToCurrencyId == target.Id);
                if (alreadyExists) continue;
                await db.ExchangeRates.AddAsync(new ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    FromCurrencyId = ils.Id,
                    ToCurrencyId = target.Id,
                    Rate = kv.Value,
                    EffectiveAt = now
                });
                created++;
            }
            if (created > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} ILS exchange rates", created);
            }
        }

        private async Task SeedWarehousesAsync(ApplicationDbContext db)
        {
            if (await db.Warehouses.AnyAsync()) return;

            var warehouses = new List<Warehouse>
            {
                new Warehouse { Id = Guid.NewGuid(), Name = "المستودع الرئيسي", Code = "WH-MAIN", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Warehouse { Id = Guid.NewGuid(), Name = "مستودع المنطقة الشرقية", Code = "WH-EAST", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
                new Warehouse { Id = Guid.NewGuid(), Name = "مستودع المنطقة الغربية", Code = "WH-WEST", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow },
            };

            await db.Warehouses.AddRangeAsync(warehouses);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} warehouses", warehouses.Count);
        }

        private async Task SeedHeroBannersAsync(ApplicationDbContext db)
        {
            var seedBanners = new List<HeroBanner>
            {
                new HeroBanner
                {
                    Id = Guid.NewGuid(),
                    BadgeText = "مجموعة جديدة 2026",
                    Title = "اكتشف منتجات مذهلة بأسعار لا تُقاوم",
                    Subtitle = "تسوق أحدث الصيحات في الإلكترونيات والأزياء والمنزل والمزيد. شحن مجاني وسريع لكافة المناطق.",
                    PrimaryButtonText = "تسوق الآن",
                    PrimaryButtonLink = "/products",
                    SecondaryButtonText = "تصفح التصنيفات",
                    SecondaryButtonLink = "/categories",
                    ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=1200&auto=format&fit=crop&q=80",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new HeroBanner
                {
                    Id = Guid.NewGuid(),
                    BadgeText = "عروض الصيف الكبرى",
                    Title = "أحدث الهواتف الذكية وأجهزة اللابتوب بخصومات تصل إلى 30%",
                    Subtitle = "ارتقِ بتجربتك الرقمية مع أحدث إصدارات آبل وسامسونج وديل مع ضمان أصلي وتوصيل سريع لباب منزلك.",
                    PrimaryButtonText = "استكشف الإلكترونيات",
                    PrimaryButtonLink = "/products?category=electronics",
                    SecondaryButtonText = "العروض الحصرية",
                    SecondaryButtonLink = "/products?sortBy=newest",
                    ImageUrl = "https://images.unsplash.com/photo-1468495244123-6c6c332eeece?w=1200&auto=format&fit=crop&q=80",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new HeroBanner
                {
                    Id = Guid.NewGuid(),
                    BadgeText = "أناقة لا مثيل لها",
                    Title = "أفخم العطور الفرنسية وأحدث تصاميم الموضة العصرية",
                    Subtitle = "استمتع بروائح ساحرة من ديور وكالفن كلاين وأحدث ملابس زارا المصممة خصيصاً لذوقك الرفيع.",
                    PrimaryButtonText = "تسوق العطور",
                    PrimaryButtonLink = "/products?category=beauty-perfumes",
                    SecondaryButtonText = "أحدث الأزياء",
                    SecondaryButtonLink = "/products?category=clothing-fashion",
                    ImageUrl = "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?w=1200&auto=format&fit=crop&q=80",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            foreach (var banner in seedBanners)
            {
                var existing = await db.HeroBanners.FirstOrDefaultAsync(b => b.Title == banner.Title);
                if (existing == null)
                {
                    await db.HeroBanners.AddAsync(banner);
                }
                else
                {
                    existing.BadgeText = banner.BadgeText;
                    existing.Subtitle = banner.Subtitle;
                    existing.PrimaryButtonText = banner.PrimaryButtonText;
                    existing.PrimaryButtonLink = banner.PrimaryButtonLink;
                    existing.SecondaryButtonText = banner.SecondaryButtonText;
                    existing.SecondaryButtonLink = banner.SecondaryButtonLink;
                    existing.ImageUrl = banner.ImageUrl;
                    existing.DisplayOrder = banner.DisplayOrder;
                    existing.IsActive = banner.IsActive;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded hero banners");
        }

        private async Task SeedStoreFeaturesAsync(ApplicationDbContext db)
        {
            if (await db.StoreFeatures.AnyAsync()) return;

            var features = new List<StoreFeature>
            {
                new StoreFeature
                {
                    Id = Guid.NewGuid(),
                    Title = "شحن مجاني وسريع",
                    Description = "شحن مجاني لجميع الطلبات المؤهلة حتى باب منزلك",
                    IconName = "Truck",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new StoreFeature
                {
                    Id = Guid.NewGuid(),
                    Title = "دفع آمن ومحمي",
                    Description = "طرق دفع متعددة مشفرة وآمنة بأعلى معايير الحماية",
                    IconName = "Shield",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new StoreFeature
                {
                    Id = Guid.NewGuid(),
                    Title = "إرجاع سهل خلال 30 يوماً",
                    Description = "إمكانية استبدال أو استرجاع المنتجات بكل سهولة وبدون تعقيد",
                    IconName = "RotateCcw",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new StoreFeature
                {
                    Id = Guid.NewGuid(),
                    Title = "دعم فني على مدار الساعة",
                    Description = "فريق خدمة عملاء متواجد للمساعدة طوال أيام الأسبوع",
                    IconName = "Headphones",
                    DisplayOrder = 4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await db.StoreFeatures.AddRangeAsync(features);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} store features", features.Count);
        }

        private async Task SeedTagsAsync(ApplicationDbContext db)
        {
            var seedTags = new List<Tag>
            {
                new Tag { Id = Guid.NewGuid(), Name = "الأكثر مبيعاً", Slug = "best-seller" },
                new Tag { Id = Guid.NewGuid(), Name = "وصل حديثاً", Slug = "new-arrivals" },
                new Tag { Id = Guid.NewGuid(), Name = "عروض حصرية", Slug = "exclusive-deals" },
                new Tag { Id = Guid.NewGuid(), Name = "خصم خاص", Slug = "special-discount" },
                new Tag { Id = Guid.NewGuid(), Name = "شحن مجاني", Slug = "free-shipping" },
                new Tag { Id = Guid.NewGuid(), Name = "أصلي 100%", Slug = "100-percent-authentic" },
                new Tag { Id = Guid.NewGuid(), Name = "تكنولوجيا ذكية", Slug = "smart-tech" },
                new Tag { Id = Guid.NewGuid(), Name = "مقاوم للماء", Slug = "waterproof" },
                new Tag { Id = Guid.NewGuid(), Name = "ألعاب وجيمنج", Slug = "gaming" },
                new Tag { Id = Guid.NewGuid(), Name = "هدايا مميزة", Slug = "gift-ideas" },
                new Tag { Id = Guid.NewGuid(), Name = "ترند الموسم", Slug = "trending" },
                new Tag { Id = Guid.NewGuid(), Name = "جودة فاخرة", Slug = "premium-quality" }
            };

            foreach (var tag in seedTags)
            {
                var existing = await db.Tags.FirstOrDefaultAsync(t => t.Slug == tag.Slug);
                if (existing == null)
                {
                    await db.Tags.AddAsync(tag);
                }
                else
                {
                    existing.Name = tag.Name;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded tags");
        }

        private async Task SeedShippingZonesAsync(ApplicationDbContext db)
        {
            if (await db.ShippingZones.AnyAsync()) return;

            var westBankZone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "الضفة الغربية",
                Description = "جميع مدن وقرى ومحافظات الضفة الغربية (رام الله، نابلس، الخليل، جنين، طولكرم، قلقيلية، بيت لحم، أريحا، سلفيت، طوباس)",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Locations = new List<ShippingZoneLocation>
                {
                    new ShippingZoneLocation { Id = Guid.NewGuid(), CountryCode = "PS", RegionCode = "WEST_BANK" }
                },
                Methods = new List<ShippingMethod>
                {
                    new ShippingMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "توصيل الضفة الغربية",
                        Description = "توصيل سريع لباب المنزل خلال 1 إلى 3 أيام عمل",
                        Type = "flat_rate",
                        BaseRate = 5.50m, // ~20 ₪
                        EstimatedDaysMin = 1,
                        EstimatedDaysMax = 3,
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                }
            };

            var jerusalemZone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "القدس وضواحيها",
                Description = "مدينة القدس وضواحيها وقرى القدس",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Locations = new List<ShippingZoneLocation>
                {
                    new ShippingZoneLocation { Id = Guid.NewGuid(), CountryCode = "PS", RegionCode = "JERUSALEM" }
                },
                Methods = new List<ShippingMethod>
                {
                    new ShippingMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "توصيل القدس وضواحيها",
                        Description = "توصيل مباشر إلى مناطق القدس وضواحيها خلال 1 إلى 2 أيام",
                        Type = "flat_rate",
                        BaseRate = 8.00m, // ~30 ₪
                        EstimatedDaysMin = 1,
                        EstimatedDaysMax = 2,
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                }
            };

            var inside48Zone = new ShippingZone
            {
                Id = Guid.NewGuid(),
                Name = "أراضي الـ 48 والداخل المحتل",
                Description = "جميع مناطق ومدن الداخل المحتل وأراضي الـ 48 (الجليل، المثلث، النقب، يافا، حيفا، عكا، تل أبيب، الناصرة، والمناطق المحيطة)",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Locations = new List<ShippingZoneLocation>
                {
                    new ShippingZoneLocation { Id = Guid.NewGuid(), CountryCode = "IL", RegionCode = "INSIDE_48" }
                },
                Methods = new List<ShippingMethod>
                {
                    new ShippingMethod
                    {
                        Id = Guid.NewGuid(),
                        Name = "توصيل أراضي الـ 48 والداخل",
                        Description = "توصيل آمن إلى كافة مناطق ومدن الداخل وأراضي 48 خلال 2 إلى 4 أيام عمل",
                        Type = "flat_rate",
                        BaseRate = 14.00m, // ~50 ₪
                        EstimatedDaysMin = 2,
                        EstimatedDaysMax = 4,
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    }
                }
            };

            await db.ShippingZones.AddRangeAsync(westBankZone, jerusalemZone, inside48Zone);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded Palestinian shipping zones and methods (West Bank, Jerusalem, Inside 48)");
        }

        private async Task SeedCategoriesAsync(ApplicationDbContext db)
        {
            var seedCategories = new List<Category>
            {
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "إلكترونيات",
                    Slug = "electronics",
                    Description = "أحدث الأجهزة الذكية والإلكترونيات الاستهلاكية وملحقاتها من كبرى الشركات العالمية",
                    ImageUrl = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 1,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "إلكترونيات",
                    MetaDescription = "تسوق أحدث الأجهزة الإلكترونية الذكية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الهواتف الذكية",
                    Slug = "smartphones",
                    Description = "أحدث الهواتف الذكية من آبل وسامسونج وشاومي مع كافة الإكسسوارات والشواحن الأصلية",
                    ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 2,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "الهواتف الذكية",
                    MetaDescription = "تسوق الهواتف الذكية الأصلية بأفضل الأسعار",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الحواسيب واللابتوبات",
                    Slug = "laptops-computers",
                    Description = "أجهزة لابتوب للأعمال والتصميم والألعاب من أبل وديل وأسوس ولينوفو",
                    ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 3,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "الحواسيب واللابتوبات",
                    MetaDescription = "تسوق أحدث أجهزة اللابتوب والكمبيوتر المكتبي",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الصوتيات والسماعات",
                    Slug = "audio-headphones",
                    Description = "سماعات رأس لاسلكية ومكبرات صوت فائقة النقاء وعازلة للضوضاء",
                    ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 4,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "الصوتيات والسماعات",
                    MetaDescription = "تسوق سماعات الرأس والأنظمة الصوتية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "ملابس وأزياء",
                    Slug = "clothing-fashion",
                    Description = "أرقى صيحات الموضة والأزياء الرجالية والنسائية العصرية لجميع الفصول",
                    ImageUrl = "https://images.unsplash.com/photo-1445205170230-053b83016050?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 5,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "ملابس وأزياء",
                    MetaDescription = "تسوق ملابس وأزياء رجالية ونسائية عصرية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "أحذية وحقائب",
                    Slug = "shoes-bags",
                    Description = "أحذية رياضية ورسمية وحقائب سفر وظهر أنيقة بجودة استثنائية",
                    ImageUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 6,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "أحذية وحقائب",
                    MetaDescription = "تسوق أحذية رياضية وحقائب ظهر وسفر أصلية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "المنزل والمطبخ",
                    Slug = "home-kitchen",
                    Description = "أجهزة تحضير القهوة وأدوات المطبخ الذكية ومستلزمات البيت العصري",
                    ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 7,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "المنزل والمطبخ",
                    MetaDescription = "تسوق مستلزمات وأجهزة المنزل والمطبخ",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "العطور والجمال",
                    Slug = "beauty-perfumes",
                    Description = "أفخم العطور الفرنسية والعالمية الأصلية ومنتجات العناية بالبشرة",
                    ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 8,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "العطور والجمال",
                    MetaDescription = "تسوق العطور الأصلية ومستحضرات الجمال",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الرياضة واللياقة",
                    Slug = "sports-fitness",
                    Description = "معدات اللياقة البدنية والتمارين المنزلية والملابس الرياضية المتينة",
                    ImageUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 9,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "الرياضة واللياقة",
                    MetaDescription = "تسوق أجهزة التمارين ومستلزمات اللياقة البدنية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الساعات والإكسسوارات",
                    Slug = "watches-accessories",
                    Description = "ساعات فاخرة ورقمية وذكية تناسب جميع الإطلالات اليومية والرسمية",
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 10,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "الساعات والإكسسوارات",
                    MetaDescription = "تسوق الساعات الفاخرة والذكية والإكسسوارات",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "ألعاب الفيديو والترفيه",
                    Slug = "gaming-consoles",
                    Description = "أجهزة ألعاب بلايستيشن وإكس بوكس وملحقات الجيمنج والشاشات الاحترافية",
                    ImageUrl = "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 11,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "ألعاب الفيديو والترفيه",
                    MetaDescription = "تسوق أجهزة ألعاب الفيديو وملحقات الجيمنج",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الأجهزة المنزلية الذكية",
                    Slug = "smart-home",
                    Description = "مكانس روبوتية ومقالي هوائية وأجهزة ذكية لتسهيل حياتك اليومية",
                    ImageUrl = "https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 12,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "الأجهزة المنزلية الذكية",
                    MetaDescription = "تسوق أجهزة المنزل الذكي",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "النظارات والبصريات",
                    Slug = "eyewear-sunglasses",
                    Description = "نظارات شمسية أصلية وتصاميم أيقونية لحماية عينيك بأناقة",
                    ImageUrl = "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 13,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "النظارات والبصريات",
                    MetaDescription = "تسوق النظارات الشمسية الفاخرة",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "العناية الشخصية والصحة",
                    Slug = "personal-care",
                    Description = "ماكينات حلاقة وأجهزة تصفيف الشعر ومستحضرات العناية الشخصية المتقدمة",
                    ImageUrl = "https://images.unsplash.com/photo-1621607512214-68297480165e?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 14,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "العناية الشخصية والصحة",
                    MetaDescription = "تسوق أجهزة ومستحضرات العناية الشخصية",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                }
            };

            foreach (var cat in seedCategories)
            {
                var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == cat.Slug);
                if (existing == null)
                {
                    await db.Categories.AddAsync(cat);
                }
                else
                {
                    existing.Name = cat.Name;
                    existing.Description = cat.Description;
                    existing.ImageUrl = cat.ImageUrl;
                    existing.IsFeatured = cat.IsFeatured;
                    existing.DisplayOrder = cat.DisplayOrder;
                    existing.IsActive = cat.IsActive;
                }
            }
            await db.SaveChangesAsync();

            // Clean up obsolete English categories and reassign any products linked to them
            var fashionCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "clothing-fashion");
            var homeCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "home-kitchen");
            var sportsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "sports-fitness");

            var obsoleteCatSlugs = new[] { "clothing", "home-garden", "sports-outdoors", "books" };
            var obsoleteCats = await db.Categories.Where(c => obsoleteCatSlugs.Contains(c.Slug)).ToListAsync();
            foreach (var oldCat in obsoleteCats)
            {
                var prods = await db.Products.Where(p => p.CategoryId == oldCat.Id).ToListAsync();
                foreach (var p in prods)
                {
                    if (oldCat.Slug == "clothing" && fashionCat != null) p.CategoryId = fashionCat.Id;
                    else if (oldCat.Slug == "home-garden" && homeCat != null) p.CategoryId = homeCat.Id;
                    else if (oldCat.Slug == "sports-outdoors" && sportsCat != null) p.CategoryId = sportsCat.Id;
                    else p.CategoryId = null;
                }
                db.Categories.Remove(oldCat);
            }
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded and synchronized categories, cleaned obsolete English categories");
        }

        private async Task SeedBrandsAsync(ApplicationDbContext db)
        {
            var seedBrands = new List<Brand>
            {
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "آبل (Apple)",
                    Slug = "apple",
                    Description = "الشركة الرائدة عالمياً في الابتكار التكنولوجي والأجهزة الذكية المتكاملة",
                    ImageUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "سامسونج (Samsung)",
                    Slug = "samsung",
                    Description = "تقنيات متطورة وشاشات مذهلة وأجهزة منزلية وهواتف ذكية رائدة عالمياً",
                    ImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "نايكي (Nike)",
                    Slug = "nike",
                    Description = "العلامة الرياضية الأولى عالمياً للأحذية والملابس الرياضية المبتكرة ذات الأداء العالي",
                    ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "أديداس (Adidas)",
                    Slug = "adidas",
                    Description = "تصاميم رياضية أيقونية وأداء استثنائي لجميع الرياضيين وعشاق الأناقة اليومية",
                    ImageUrl = "https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "سوني (Sony)",
                    Slug = "sony",
                    Description = "صوتيات احترافية ومنصات ألعاب بلايستيشن وكاميرات وتقنيات ترفيهية رائدة",
                    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "زارا (Zara)",
                    Slug = "zara",
                    Description = "أحدث خطوط الموضة والأزياء الأوروبية الراقية للرجال والنساء",
                    ImageUrl = "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "ديور (Dior)",
                    Slug = "dior",
                    Description = "دار الأزياء والعطور الفرنسية الفاخرة ذات اللمسات الأسطورية التي لا تُنسى",
                    ImageUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "ديل (Dell)",
                    Slug = "dell",
                    Description = "حواسيب محمولة ومكتبية قوية وشاشات متميزة للمحترفين والمصممين",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "شاومي (Xiaomi)",
                    Slug = "xiaomi",
                    Description = "أجهزة ذكية متطورة وهواتف رائدة وأجهزة منزلية عملية بقيمة لا تُضاهى",
                    ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "أسوس (Asus)",
                    Slug = "asus",
                    Description = "أجهزة جيمنج ولابتوبات روج زيفيروس الفائقة للمحترفين وهواة الألعاب",
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "لينوفو (Lenovo)",
                    Slug = "lenovo",
                    Description = "أجهزة ThinkPad وLegion القوية للإنتاجية العالية والألعاب",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "أنكر (Anker)",
                    Slug = "anker",
                    Description = "العلامة الأولى عالمياً في ملحقات الشحن السريع والشواحن اللاسلكية والباور بانك",
                    ImageUrl = "https://images.unsplash.com/photo-1609592424364-c289069d2f2d?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "فيليبس (Philips)",
                    Slug = "philips",
                    Description = "حلول ذكية للمنزل والمطبخ وأجهزة العناية الشخصية المبتكرة",
                    ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "بوما (Puma)",
                    Slug = "puma",
                    Description = "أحذية وملابس رياضية عصرية مستوحاة من ثقافة الشارع والأداء العالي",
                    ImageUrl = "https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "كاسيو (Casio)",
                    Slug = "casio",
                    Description = "ساعات جي شوك اليابانية الأسطورية المقاومة للصدمات والمياه",
                    ImageUrl = "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "رايزر (Razer)",
                    Slug = "razer",
                    Description = "أجهزة وملحقات الألعاب الاحترافية المصممة للاعبين بواسطة لاعبين",
                    ImageUrl = "https://images.unsplash.com/photo-1612287232230-e1a5f69be844?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "نسبريسو (Nespresso)",
                    Slug = "nespresso",
                    Description = "ماكينات وكبسولات القهوة السويسرية الفاخرة للاستمتاع بكوب قهوة استثنائي",
                    ImageUrl = "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "لوريال باريس (L'Oréal)",
                    Slug = "loreal",
                    Description = "منتجات العناية بالبشرة والشعر والمستحضرات التجميلية الرائدة عالمياً",
                    ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "ريبان (Ray-Ban)",
                    Slug = "ray-ban",
                    Description = "النظارات الشمسية والطبية الإيطالية الأيقونية بتصاميم أفياتور ووايفرر الخالدة",
                    ImageUrl = "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "كالفن كلاين (Calvin Klein)",
                    Slug = "calvin-klein",
                    Description = "دار الأزياء والعطور الأمريكية الراقية ذات التصاميم الأنيقة والمعاصرة",
                    ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=300&auto=format&fit=crop&q=80",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                }
            };

            foreach (var brand in seedBrands)
            {
                var existing = await db.Brands.FirstOrDefaultAsync(b => b.Slug == brand.Slug);
                if (existing == null)
                {
                    await db.Brands.AddAsync(brand);
                }
                else
                {
                    existing.Name = brand.Name;
                    existing.Description = brand.Description;
                    existing.ImageUrl = brand.ImageUrl;
                    existing.IsActive = brand.IsActive;
                }
            }
            await db.SaveChangesAsync();

            // Clean up obsolete English placeholder brands
            var obsoleteBrandSlugs = new[] { "techbrand", "fashionco", "homeessentials" };
            var obsoleteBrands = await db.Brands.Where(b => obsoleteBrandSlugs.Contains(b.Slug)).ToListAsync();
            foreach (var oldBrand in obsoleteBrands)
            {
                var prods = await db.Products.Where(p => p.BrandId == oldBrand.Id).ToListAsync();
                foreach (var p in prods)
                {
                    p.BrandId = null;
                }
                db.Brands.Remove(oldBrand);
            }
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded and synchronized brands, cleaned obsolete English brands");
        }

        private async Task SeedProductsAsync(ApplicationDbContext db)
        {
            var mainWarehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "WH-MAIN")
                                ?? await db.Warehouses.FirstOrDefaultAsync();
            if (mainWarehouse == null) return;

            var electronicsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "electronics");
            var smartphonesCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "smartphones") ?? electronicsCat;
            var laptopsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "laptops-computers") ?? electronicsCat;
            var audioCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "audio-headphones") ?? electronicsCat;
            var fashionCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "clothing-fashion");
            var shoesCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "shoes-bags");
            var homeCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "home-kitchen");
            var beautyCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "beauty-perfumes");
            var sportsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "sports-fitness");
            var watchesCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "watches-accessories");
            var gamingCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "gaming-consoles") ?? electronicsCat;
            var smartHomeCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "smart-home") ?? homeCat;
            var eyewearCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "eyewear-sunglasses") ?? watchesCat;
            var personalCareCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "personal-care") ?? beautyCat;

            var appleBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "apple");
            var samsungBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "samsung");
            var nikeBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "nike");
            var adidasBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "adidas");
            var sonyBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "sony");
            var zaraBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "zara");
            var diorBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "dior");
            var dellBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "dell");
            var xiaomiBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "xiaomi");
            var asusBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "asus");
            var lenovoBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "lenovo");
            var ankerBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "anker");
            var philipsBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "philips");
            var pumaBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "puma");
            var casioBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "casio");
            var razerBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "razer");
            var nespressoBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "nespresso");
            var lorealBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "loreal");
            var raybanBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "ray-ban");
            var calvinBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "calvin-klein");

            var attributeCache = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);

            var productsToSeed = new List<(
                Product product,
                List<(string url, bool isPrimary, string alt)> images,
                List<(string name, string sku, decimal price, decimal compareAt, (string attribute, string code, string value)[] options)> variants,
                int stock
            )>
            {
                // 1. iPhone 15 Pro Max
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = smartphonesCat?.Id ?? electronicsCat?.Id,
                        BrandId = appleBrand?.Id,
                        Name = "آيفون 15 برو ماكس (iPhone 15 Pro Max)",
                        Slug = "iphone-15-pro-max",
                        Sku = "APL-IP15PM-256",
                        ShortDescription = "أقوى هاتف من آبل بهيكل التيتانيوم وشريحة A17 Pro الخارقة وكاميرا 5X تقريب بصري.",
                        Description = "يأتي هاتف آيفون 15 برو ماكس بتصميم متطور من التيتانيوم المستخدم في صناعة الطيران والفضاء، مما يجعله أخف وزناً وأقوى متانة. مزود بشريحة A17 Pro الثورية التي تقدم أداءً لا مثيل له للألعاب والمهام الاحترافية، مع نظام كاميرات متطور يدعم دقة 48 ميجابكسل وتقريب بصري حتى 5 أضعاف.",
                        BasePrice = 1199m,
                        CostPrice = 899m,
                        CompareAtPrice = 1299m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AllowBackorder = true,
                        AverageRating = 4.9m,
                        ReviewCount = 38,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=80", true, "آيفون 15 برو ماكس من الأمام والخلف"),
                        ("https://images.unsplash.com/photo-1695048065059-d2d8ceeb0f2c?w=800&auto=format&fit=crop&q=80", false, "آيفون 15 برو ماكس تيتانيوم طبيعي"),
                        ("https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=800&auto=format&fit=crop&q=80", false, "شاشة آيفون فائقة السطوع")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("تيتانيوم طبيعي 256 جيجابايت", "APL-IP15PM-256-NAT", 1199m, 1299m, new[] { ("اللون", "COLOR", "تيتانيوم طبيعي"), ("سعة التخزين", "STORAGE", "256 جيجابايت") }),
                        ("تيتانيوم أسود 512 جيجابايت", "APL-IP15PM-512-BLK", 1399m, 1499m, new[] { ("اللون", "COLOR", "تيتانيوم أسود"), ("سعة التخزين", "STORAGE", "512 جيجابايت") }),
                        ("تيتانيوم أزرق 1 تيرابايت", "APL-IP15PM-1TB-BLU", 1599m, 1699m, new[] { ("اللون", "COLOR", "تيتانيوم أزرق"), ("سعة التخزين", "STORAGE", "1 تيرابايت") })
                    },
                    45
                ),

                // 2. Sony WH-1000XM5
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = audioCat?.Id ?? electronicsCat?.Id,
                        BrandId = sonyBrand?.Id,
                        Name = "سماعات سوني اللاسلكية WH-1000XM5 عازلة للضوضاء",
                        Slug = "sony-wh-1000xm5-headphones",
                        Sku = "SNY-WH1000XM5",
                        ShortDescription = "سماعات رأس لاسلكية متميزة بإلغاء الضوضاء الرائد في الصناعة وصوت عالي الدقة وبطارية تدوم 30 ساعة.",
                        Description = "تعيد سماعات الرأس اللاسلكية WH-1000XM5 من سوني كتابة قواعد الاستماع بدون تشتيت، بفضل معالجين متطورين و8 ميكروفونات لعزل الضوضاء تلقائياً. توفر راحة استثنائية عند ارتدائها طوال اليوم مع جودة مكالمات فائقة الوضوح وشحن سريع يمنحك 3 ساعات تشغيل خلال 3 دقائق فقط.",
                        BasePrice = 349m,
                        CostPrice = 220m,
                        CompareAtPrice = 399m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 24,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80", true, "سماعات سوني WH-1000XM5"),
                        ("https://images.unsplash.com/photo-1484704849700-f032a568e944?w=800&auto=format&fit=crop&q=80", false, "تفاصيل سماعة الرأس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود كلاسيكي (Black)", "SNY-WH1000XM5-BLK", 349m, 399m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("فضي بلاتيني (Silver)", "SNY-WH1000XM5-SLV", 349m, 399m, new[] { ("اللون", "COLOR", "فضي بلاتيني") })
                    },
                    60
                ),

                // 3. Nike Air Max Plus
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = shoesCat?.Id,
                        BrandId = nikeBrand?.Id,
                        Name = "حذاء نايكي إير ماكس بلس الرياضي (Nike Air Max Plus)",
                        Slug = "nike-air-max-plus-sneakers",
                        Sku = "NKE-AIRMAX-PLUS",
                        ShortDescription = "حذاء رياضي أنيق ومريح بتقنية Tuned Air لتوفير ثبات وتوسيد فائق أثناء الركض والمشي.",
                        Description = "يتميز حذاء نايكي إير ماكس بلس بتصميم أيقوني مستوحى من أشجار النخيل وأمواج المحيط، مع تقنية Tuned Air التي تقدم توسيداً خفيفاً واستقراراً مذهلاً في كل خطوة. الجزء العلوي خفيف الوزن وجيد التهوية مع نعل مطاطي متين يوفر ثباتاً ممتازاً على مختلف الأسطح.",
                        BasePrice = 175m,
                        CostPrice = 95m,
                        CompareAtPrice = 199m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 19,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&auto=format&fit=crop&q=80", true, "حذاء نايكي إير ماكس أحمر"),
                        ("https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=800&auto=format&fit=crop&q=80", false, "تفاصيل حذاء نايكي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("مقاس 41 - أحمر/أسود", "NKE-AIRMAX-41", 175m, 199m, new[] { ("المقاس", "SIZE", "41"), ("اللون", "COLOR", "أحمر/أسود") }),
                        ("مقاس 42 - أحمر/أسود", "NKE-AIRMAX-42", 175m, 199m, new[] { ("المقاس", "SIZE", "42"), ("اللون", "COLOR", "أحمر/أسود") }),
                        ("مقاس 43 - أحمر/أسود", "NKE-AIRMAX-43", 175m, 199m, new[] { ("المقاس", "SIZE", "43"), ("اللون", "COLOR", "أحمر/أسود") }),
                        ("مقاس 44 - أحمر/أسود", "NKE-AIRMAX-44", 175m, 199m, new[] { ("المقاس", "SIZE", "44"), ("اللون", "COLOR", "أحمر/أسود") })
                    },
                    80
                ),

                // 4. Apple Watch Series 9
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = watchesCat?.Id,
                        BrandId = appleBrand?.Id,
                        Name = "ساعة أبل الذكية الجيل التاسع (Apple Watch Series 9 GPS)",
                        Slug = "apple-watch-series-9",
                        Sku = "APL-WATCH-S9",
                        ShortDescription = "ساعة ذكية بشريحة S9 فائقة السرعة وإيماءة الضغط المزدوج المبتكرة ومستشعرات صحية متقدمة.",
                        Description = "تأتي ساعة Apple Watch Series 9 بقوة شريحة S9 SiP المخصصة من أبل مع شاشة فائقة السطوع تصل إلى 2000 شمعة. استمتع بطريقة سحرية للتفاعل بدون لمس الشاشة عبر حركة الضغط المزدوج بالأصابع، مع مراقبة متواصلة لمعدل نبضات القلب وتخطيط القلب ونسبة الأكسجين في الدم وتتبع النوم والتمارين الرياضية.",
                        BasePrice = 399m,
                        CostPrice = 280m,
                        CompareAtPrice = 429m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 29,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", true, "ساعة يد ذكية"),
                        ("https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=800&auto=format&fit=crop&q=80", false, "ساعة أبل بالمعصم")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("هيكل ألمنيوم 41 مم - سماء الليل", "APL-W-S9-41-MID", 399m, 429m, new[] { ("المقاس", "SIZE", "41 مم"), ("اللون", "COLOR", "سماء الليل") }),
                        ("هيكل ألمنيوم 45 مم - ضوء النجوم", "APL-W-S9-45-STR", 429m, 459m, new[] { ("المقاس", "SIZE", "45 مم"), ("اللون", "COLOR", "ضوء النجوم") })
                    },
                    35
                ),

                // 5. Dior Sauvage
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = beautyCat?.Id,
                        BrandId = diorBrand?.Id,
                        Name = "عطر سوفاج ديور أو دو بارفان (Dior Sauvage EDP)",
                        Slug = "dior-sauvage-edp",
                        Sku = "DIOR-SAUVAGE-EDP",
                        ShortDescription = "عطر رجالي شرقي منعش وجريء يمزج نفحات البرغموت الكالابري مع خشب الصندل والفانيليا الجذابة.",
                        Description = "عطر سوفاج أو دو بارفان من دار ديور الفرنسية هو تحفة عطرية مستوحاة من سحر الصحراء في ساعة الغسق. تفتتح الرائحة بانتعاش البرغموت المنعش مع لمسات حارة من جوزة الطيب والينسون النجمي، وتستقر على قاعدة غنية من الفانيليا وخشب الصندل التي تدوم طويلاً وتترك انطباعاً ساحراً لا يُنسى.",
                        BasePrice = 145m,
                        CostPrice = 85m,
                        CompareAtPrice = 165m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 5.0m,
                        ReviewCount = 42,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1541643600914-78b084683601?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر سوفاج ديور"),
                        ("https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&auto=format&fit=crop&q=80", false, "عطر فاخر أصلي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("حجم 60 مل", "DIOR-SVG-60ML", 110m, 125m, new[] { ("الحجم", "VOLUME", "60 مل") }),
                        ("حجم 100 مل", "DIOR-SVG-100ML", 145m, 165m, new[] { ("الحجم", "VOLUME", "100 مل") }),
                        ("حجم 200 مل", "DIOR-SVG-200ML", 210m, 240m, new[] { ("الحجم", "VOLUME", "200 مل") })
                    },
                    70
                ),

                // 6. Zara Puffer Jacket
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = fashionCat?.Id,
                        BrandId = zaraBrand?.Id,
                        Name = "جاكيت زارا شتوي مبطن مقاوم للرياح والماء (Zara Puffer Jacket)",
                        Slug = "zara-winter-puffer-jacket",
                        Sku = "ZRA-PUFF-JKT",
                        ShortDescription = "جاكيت شتوي دافئ وعصري ببطانة حرارية عازلة وقماش معالج لمقاومة المطر والرياح الباردة.",
                        Description = "صُمم هذا الجاكيت من زارا ليمنحك الدفء التام والأناقة العصرية في الأيام الباردة والممطرة. يحتوي على سحاب أمامي متين مع ياقة مرتفعة وغطاء رأس قابل للتعديل، بالإضافة إلى جيوب جانبية دافئة مبطنة بطبقة ناعمة.",
                        BasePrice = 89m,
                        CostPrice = 45m,
                        CompareAtPrice = 120m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.6m,
                        ReviewCount = 15,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1544441893-675973e31985?w=800&auto=format&fit=crop&q=80", true, "جاكيت شتوي أنيق"),
                        ("https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=800&auto=format&fit=crop&q=80", false, "ملابس زارا الشتوية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود - مقاس M", "ZRA-PUFF-BLK-M", 89m, 120m, new[] { ("اللون", "COLOR", "أسود"), ("المقاس", "SIZE", "M") }),
                        ("أسود - مقاس L", "ZRA-PUFF-BLK-L", 89m, 120m, new[] { ("اللون", "COLOR", "أسود"), ("المقاس", "SIZE", "L") }),
                        ("زيتي - مقاس M", "ZRA-PUFF-OLV-M", 89m, 120m, new[] { ("اللون", "COLOR", "زيتي"), ("المقاس", "SIZE", "M") }),
                        ("زيتي - مقاس L", "ZRA-PUFF-OLV-L", 89m, 120m, new[] { ("اللون", "COLOR", "زيتي"), ("المقاس", "SIZE", "L") })
                    },
                    90
                ),

                // 7. Dell XPS 15 Laptop
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = laptopsCat?.Id ?? electronicsCat?.Id,
                        BrandId = dellBrand?.Id,
                        Name = "لابتوب ديل XPS 15 إنش شاشة 3.5K OLED (Dell XPS 15)",
                        Slug = "dell-xps-15-oled-laptop",
                        Sku = "DLL-XPS15-OLED",
                        ShortDescription = "حاسوب محمول فائق الأداء بمعالج Intel Core i7 وبطاقة شاشة RTX 4060 وشاشة OLED مذهلة.",
                        Description = "يعد Dell XPS 15 الخيار المثالي للمصممين والمبرمجين وصناع المحتوى، حيث يجمع بين هيكل أنيق من الألمنيوم وألياف الكربون، وشاشة OLED مذهلة بدقة 3.5K تدعم نطاق ألوان DCI-P3 بنسبة 100%، مع بطارية قوية ونظام تبريد متقدم لأقصى إنتاجية.",
                        BasePrice = 1499m,
                        CostPrice = 1100m,
                        CompareAtPrice = 1699m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 12,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80", true, "لابتوب ديل XPS 15"),
                        ("https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80", false, "شاشة ديل فائقة النقاء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("معالج i7 - رام 16GB - سعة 512GB SSD", "DLL-XPS15-16-512", 1499m, 1699m, new[] { ("المواصفات", "SPEC", "i7 / 16GB / 512GB SSD") }),
                        ("معالج i9 - رام 32GB - سعة 1TB SSD", "DLL-XPS15-32-1TB", 1899m, 2099m, new[] { ("المواصفات", "SPEC", "i9 / 32GB / 1TB SSD") })
                    },
                    25
                ),

                // 8. Samsung 55" 4K Smart TV
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = electronicsCat?.Id,
                        BrandId = samsungBrand?.Id,
                        Name = "تلفزيون سامسونج الذكي 55 بوصة OLED 4K (Samsung Smart TV)",
                        Slug = "samsung-55-inch-oled-4k-tv",
                        Sku = "SMS-TV-55OLED",
                        ShortDescription = "تلفزيون سامسونج أوليد بدقة 4K فائقة الوضوح مع معالج Neural Quantum ومعدل تحديث 120Hz.",
                        Description = "استمتع بتجربة سينمائية لا مثيل لها في منزلك مع تلفزيون سامسونج OLED مقاس 55 بوصة. درجات سواد لا نهائية وألوان مفعمة بالحياة بفضل تقنية النقاط الكمية Quantum Dot، مع دعم كامل لتقنية Dolby Atmos وتجربة ألعاب سلسة ومذهلة بمعدل تحديث 120 هرتز.",
                        BasePrice = 1199m,
                        CostPrice = 850m,
                        CompareAtPrice = 1499m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 18,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=800&auto=format&fit=crop&q=80", true, "شاشة تلفزيون سامسونج ذكية"),
                        ("https://images.unsplash.com/photo-1461151304267-38535e780c79?w=800&auto=format&fit=crop&q=80", false, "غرفة جلوس مع تلفزيون سامسونج")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    20
                ),

                // 9. Adidas Ultraboost 1.0
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = shoesCat?.Id,
                        BrandId = adidasBrand?.Id,
                        Name = "حذاء أديداس ألترا بوست 1.0 للجري (Adidas Ultraboost 1.0)",
                        Slug = "adidas-ultraboost-1-sneakers",
                        Sku = "ADS-UB-10",
                        ShortDescription = "حذاء الجري الأكثر راحة في العالم بنعل Boost الثوري وجزء علوي محبوك من Primeknit.",
                        Description = "سواء كنت تمارس الجري في الصباح أو تتنقل في مشاويرك اليومية، يمنحك حذاء Adidas Ultraboost 1.0 طاقة متجددة في كل خطوة بفضل مئات كبسولات Boost المدمجة في النعل. الجزء العلوي يحتضن القدم بنعومة ودعم مثالي.",
                        BasePrice = 180m,
                        CostPrice = 95m,
                        CompareAtPrice = 210m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 22,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=800&auto=format&fit=crop&q=80", true, "حذاء أديداس ألترا بوست أبيض"),
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", false, "تفاصيل نعل ألترا بوست")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أبيض ناصع - مقاس 42", "ADS-UB-WHT-42", 180m, 210m, new[] { ("اللون", "COLOR", "أبيض ناصع"), ("المقاس", "SIZE", "42") }),
                        ("أبيض ناصع - مقاس 43", "ADS-UB-WHT-43", 180m, 210m, new[] { ("اللون", "COLOR", "أبيض ناصع"), ("المقاس", "SIZE", "43") }),
                        ("أسود كور - مقاس 42", "ADS-UB-BLK-42", 180m, 210m, new[] { ("اللون", "COLOR", "أسود كور"), ("المقاس", "SIZE", "42") }),
                        ("أسود كور - مقاس 44", "ADS-UB-BLK-44", 180m, 210m, new[] { ("اللون", "COLOR", "أسود كور"), ("المقاس", "SIZE", "44") })
                    },
                    65
                ),

                // 10. De'Longhi Dedica Espresso Machine
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = homeCat?.Id,
                        BrandId = nespressoBrand?.Id,
                        Name = "ماكينة إعداد الإسبريسو الإيطالية ديلونجي ديديكا (De'Longhi Dedica)",
                        Slug = "delonghi-dedica-espresso-machine",
                        Sku = "DLG-DEDICA-EC685",
                        ShortDescription = "ماكينة قهوة أنيقة ومضغوطة بضغط 15 بار ونظام تسخين سريع وعصا تبخير الحليب الاحترافية.",
                        Description = "استمتع بكوب قهوة إسبريسو وكابتشينو مثالي بجودة المقاهي الإيطالية في راحة منزلك. تتميز ماكينة ديلونجي ديديكا بتصميم نحيف بعرض 15 سم فقط يناسب أي مطبخ، مع نظام تسخين Thermoblock جاهز للاستخدام خلال 40 ثانية.",
                        BasePrice = 279m,
                        CostPrice = 170m,
                        CompareAtPrice = 320m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 17,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", true, "ماكينة قهوة إسبريسو"),
                        ("https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=800&auto=format&fit=crop&q=80", false, "فنجان قهوة محضر بالماكينة")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    30
                ),

                // 11. Smart Anti-Theft Backpack
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = shoesCat?.Id,
                        BrandId = null,
                        Name = "حقيبة ظهر ذكية مضادة للماء والسرقة مع منفذ شحن USB",
                        Slug = "smart-anti-theft-laptop-backpack",
                        Sku = "BAG-SMART-ANTITHEFT",
                        ShortDescription = "حقيبة لابتوب متطورة بقفل أمان وسحابات مخفية وخامة مقاومة للمطر والخدوش وسعة 35 لتر.",
                        Description = "الحقيبة المثالية للسفر والعمل والجامعة. تتسع لحاسوب محمول حتى 15.6 إنش مع جيوب مبطنة متعددة لحماية الأجهزة اللوحية والملحقات، وتأتي بمنفذ USB مدمج لشحن هاتفك أثناء التنقل بكل سهولة.",
                        BasePrice = 49m,
                        CostPrice = 22m,
                        CompareAtPrice = 69m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.6m,
                        ReviewCount = 31,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&auto=format&fit=crop&q=80", true, "حقيبة ظهر ذكية سوداء"),
                        ("https://images.unsplash.com/photo-1622560480605-d83c853bc5c3?w=800&auto=format&fit=crop&q=80", false, "تفاصيل الجيوب والملحقات")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    100
                ),

                // 12. Adjustable Dumbbell Set 24kg
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = sportsCat?.Id,
                        BrandId = null,
                        Name = "مجموعة أثقال دمبلز ذكية قابلة للتعديل حتى 24 كجم للتمارين المنزلية",
                        Slug = "adjustable-dumbbell-set-24kg",
                        Sku = "SPT-DUMBBELL-24KG",
                        ShortDescription = "دمبل ذكي يوفر 15 وزناً مختلفاً في أداة واحدة من 2.5 كجم إلى 24 كجم بنظام قفل أمان سريع.",
                        Description = "وداعاً للازدحام في الصالة الرياضية! يوفر لك هذا الدمبل الذكي القابل للتعديل تجربة تمرين شاملة لجميع عضلات الجسم. بنقرة واحدة من القرص الدوار، يمكنك تغيير الوزن بسهولة من 2.5 كجم حتى 24 كجم ليتناسب مع مستوى تدريبك.",
                        BasePrice = 199m,
                        CostPrice = 120m,
                        CompareAtPrice = 249m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 14,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", true, "دمبل تمارين رياضية"),
                        ("https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=800&auto=format&fit=crop&q=80", false, "تدريب لياقة وأثقال")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),

                // 13. Samsung Galaxy S24 Ultra
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = smartphonesCat?.Id ?? electronicsCat?.Id,
                        BrandId = samsungBrand?.Id,
                        Name = "سامسونج جالكسي S24 ألترا الذكي (Samsung Galaxy S24 Ultra)",
                        Slug = "samsung-galaxy-s24-ultra",
                        Sku = "SMS-S24U-256",
                        ShortDescription = "هاتف سامسونج الرائد بميزات الذكاء الاصطناعي Galaxy AI وهيكل التيتانيوم وقلم S Pen مدمج.",
                        Description = "استكشف آفاقاً جديدة مع هاتف Samsung Galaxy S24 Ultra المزود بميزات الذكاء الاصطناعي الثورية مثل الترجمة الفورية والبحث عبر دائرة على الشاشة ومساعد الصور الذكي. يتميز بشاشة Dynamic AMOLED 2X مسطحة مقاس 6.8 بوصة وكاميرا احترافية بدقة 200 ميجابكسل.",
                        BasePrice = 1299m,
                        CostPrice = 950m,
                        CompareAtPrice = 1399m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AllowBackorder = true,
                        AverageRating = 4.9m,
                        ReviewCount = 35,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=800&auto=format&fit=crop&q=80", true, "سامسونج جالكسي S24 ألترا"),
                        ("https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800&auto=format&fit=crop&q=80", false, "تفاصيل كاميرا سامسونج")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("رمادي تيتانيوم 256 جيجابايت", "SMS-S24U-256-GRY", 1299m, 1399m, new[] { ("اللون", "COLOR", "رمادي تيتانيوم"), ("سعة التخزين", "STORAGE", "256 جيجابايت") }),
                        ("أسود تيتانيوم 512 جيجابايت", "SMS-S24U-512-BLK", 1450m, 1550m, new[] { ("اللون", "COLOR", "أسود تيتانيوم"), ("سعة التخزين", "STORAGE", "512 جيجابايت") }),
                        ("بنفسجي تيتانيوم 512 جيجابايت", "SMS-S24U-512-VIO", 1450m, 1550m, new[] { ("اللون", "COLOR", "بنفسجي تيتانيوم"), ("سعة التخزين", "STORAGE", "512 جيجابايت") })
                    },
                    50
                ),

                // 14. MacBook Pro 16" M3 Pro
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = laptopsCat?.Id ?? electronicsCat?.Id,
                        BrandId = appleBrand?.Id,
                        Name = "ماك بوك برو 16 إنش شريحة M3 Pro الخارقة (MacBook Pro 16 M3 Pro)",
                        Slug = "macbook-pro-16-m3-pro",
                        Sku = "APL-MBP16-M3P",
                        ShortDescription = "لابتوب أبل الاحترافي للمبدعين والمبرمجين مع شاشة Liquid Retina XDR وبطارية تدوم 22 ساعة.",
                        Description = "يقدم جهاز MacBook Pro مقاس 16 بوصة أداءً استثنائياً بفضل شريحة M3 Pro المتطورة، مع شاشة Liquid Retina XDR فائقة السطوع ونظام صوتي مكون من 6 مكبرات صوت مع دعم الصوت المكاني، وبطارية مذهلة تمنحك حتى 22 ساعة عمل متواصل.",
                        BasePrice = 2499m,
                        CostPrice = 1900m,
                        CompareAtPrice = 2699m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 5.0m,
                        ReviewCount = 27,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80", true, "ماك بوك برو 16 إنش"),
                        ("https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800&auto=format&fit=crop&q=80", false, "هيكل ماك بوك برو من الألمنيوم")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود فلكي - 18GB رام - 512GB SSD", "APL-MBP16-18-512-BLK", 2499m, 2699m, new[] { ("اللون", "COLOR", "أسود فلكي"), ("المواصفات", "SPEC", "18GB RAM / 512GB SSD") }),
                        ("فضي - 36GB رام - 1TB SSD", "APL-MBP16-36-1TB-SLV", 2899m, 3099m, new[] { ("اللون", "COLOR", "فضي"), ("المواصفات", "SPEC", "36GB RAM / 1TB SSD") })
                    },
                    20
                ),

                // 15. Asus ROG Zephyrus G16 Gaming Laptop
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = gamingCat?.Id ?? laptopsCat?.Id ?? electronicsCat?.Id,
                        BrandId = asusBrand?.Id,
                        Name = "لابتوب الألعاب أسوس روج زيفيروس G16 (Asus ROG Zephyrus G16)",
                        Slug = "asus-rog-zephyrus-g16",
                        Sku = "ASUS-ROG-G16-RTX",
                        ShortDescription = "لابتوب ألعاب خارق بشاشة OLED 240Hz ومعالج Intel Core Ultra 9 وبطاقة RTX 4070.",
                        Description = "صُمم لابتوب Asus ROG Zephyrus G16 ليقدم أقصى درجات القوة في هيكل فائق النحافة من الألمنيوم المصقول. مع شاشة ROG Nebula OLED بدقة 2.5K ومعدل تحديث 240Hz وتقنية تبريد سائل ذكية للألعاب الثقيلة وصناعة المحتوى ثلاثي الأبعاد.",
                        BasePrice = 1999m,
                        CostPrice = 1550m,
                        CompareAtPrice = 2299m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 16,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80", true, "لابتوب ألعاب أسوس روج"),
                        ("https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&auto=format&fit=crop&q=80", false, "إضاءة كيبورد RGB للألعاب")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("رمادي إكليبس - RTX 4070 - 16GB / 1TB SSD", "ASUS-G16-4070-GRY", 1999m, 2299m, new[] { ("اللون", "COLOR", "رمادي إكليبس"), ("كرت الشاشة", "GPU", "RTX 4070") }),
                        ("أبيض بلاتيني - RTX 4080 - 32GB / 2TB SSD", "ASUS-G16-4080-WHT", 2499m, 2799m, new[] { ("اللون", "COLOR", "أبيض بلاتيني"), ("كرت الشاشة", "GPU", "RTX 4080") })
                    },
                    18
                ),

                // 16. Apple AirPods Pro 2
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = audioCat?.Id ?? electronicsCat?.Id,
                        BrandId = appleBrand?.Id,
                        Name = "سماعات أبل إيربودز برو الجيل الثاني USB-C (AirPods Pro 2)",
                        Slug = "apple-airpods-pro-2-usbc",
                        Sku = "APL-AIRPODS-PRO2",
                        ShortDescription = "سماعات أبل اللاسلكية الرائدة بإلغاء ضوضاء نشط مضاعف وصوت تكيفي ومنفذ USB-C.",
                        Description = "تقدم سماعات AirPods Pro الجيل الثاني مستوى استثنائياً من عزل الضوضاء النشط والصوت التكيفي الذي يمزج بين شفافية الصوت وإلغاء الضوضاء بذكاء حسب البيئة المحيطة. مقاومة للغبار والماء والعرق بمعيار IP54 مع علبة MagSafe المتطورة.",
                        BasePrice = 249m,
                        CostPrice = 175m,
                        CompareAtPrice = 279m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 48,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800&auto=format&fit=crop&q=80", true, "سماعات أبل إيربودز برو 2"),
                        ("https://images.unsplash.com/photo-1572569511254-d8f925fe2cbb?w=800&auto=format&fit=crop&q=80", false, "علبة شحن إيربودز برو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    85
                ),

                // 17. Razer BlackShark V2 Pro Gaming Headset
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = audioCat?.Id ?? gamingCat?.Id ?? electronicsCat?.Id,
                        BrandId = razerBrand?.Id,
                        Name = "سماعات الألعاب اللاسلكية رايزر بلاك شارك V2 برو (Razer BlackShark V2 Pro)",
                        Slug = "razer-blackshark-v2-pro",
                        Sku = "RZR-BSV2P-WL",
                        ShortDescription = "سماعة ألعاب تنافسية احترافية بمحركات TriForce Titanium 50mm وميكروفون فائق النقاء.",
                        Description = "إذا كانت الرياضات الإلكترونية هي شغفك، فإن Razer BlackShark V2 Pro هي سلاحك المفضل. عزل صوتي متفوق وراحة لا تضاهى لوسائد الأذن من رغوة الذاكرة المسامية وبطارية قوية تدوم حتى 70 ساعة من اللعب المتواصل.",
                        BasePrice = 199m,
                        CostPrice = 125m,
                        CompareAtPrice = 229m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 20,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1612287232230-e1a5f69be844?w=800&auto=format&fit=crop&q=80", true, "سماعة جيمنج رايزر"),
                        ("https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=800&auto=format&fit=crop&q=80", false, "سماعة رأس لاسلكية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود كلاسيكي", "RZR-BSV2P-BLK", 199m, 229m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أبيض ميركوري", "RZR-BSV2P-WHT", 199m, 229m, new[] { ("اللون", "COLOR", "أبيض ميركوري") })
                    },
                    45
                ),

                // 18. Sony PlayStation 5 Slim
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = gamingCat?.Id ?? electronicsCat?.Id,
                        BrandId = sonyBrand?.Id,
                        Name = "جهاز ألعاب بلايستيشن 5 سليم سعة 1 تيرابايت (Sony PlayStation 5 Slim)",
                        Slug = "sony-playstation-5-slim",
                        Sku = "SNY-PS5-SLIM-1TB",
                        ShortDescription = "منصة الألعاب الأكثر شعبية في العالم بتصميم نحيف وسعة تخزين 1TB ودعم رسومات 4K 120Hz.",
                        Description = "عش تجربة ألعاب غامرة لم يسبق لها مثيل مع جهاز PS5 Slim. استمتع بأوقات تحميل شبه فورية مع وحدة تخزين SSD فائقة السرعة، وردود فعل لمسية غامرة ومحفزات تكيفية مع ذراع التحكم اللاسلكي DualSense وصوت ثلاثي الأبعاد Tempest 3D.",
                        BasePrice = 499m,
                        CostPrice = 420m,
                        CompareAtPrice = 549m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 52,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=800&auto=format&fit=crop&q=80", true, "جهاز بلايستيشن 5 مع يد التحكم"),
                        ("https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=800&auto=format&fit=crop&q=80", false, "يد تحكم بلايستيشن 5 DualSense")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("النسخة الرقمية 1 تيرابايت (Digital Edition)", "SNY-PS5-SLIM-DIG", 449m, 499m, new[] { ("الإصدار", "EDITION", "النسخة الرقمية") }),
                        ("نسخة محرك الأقراص 1 تيرابايت (Disc Edition)", "SNY-PS5-SLIM-DISC", 499m, 549m, new[] { ("الإصدار", "EDITION", "نسخة محرك الأقراص") })
                    },
                    35
                ),

                // 19. Samsung Odyssey OLED G9 Curved Gaming Monitor
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = gamingCat?.Id ?? electronicsCat?.Id,
                        BrandId = samsungBrand?.Id,
                        Name = "شاشة ألعاب سامسونج أوديسي OLED G9 منحنية 49 بوصة (Odyssey OLED G9)",
                        Slug = "samsung-odyssey-oled-g9-monitor",
                        Sku = "SMS-MON-G9-OLED",
                        ShortDescription = "شاشة ألعاب فائقة العرض 49 إنش Dual QHD بمعدل تحديث 240Hz وزمن استجابة 0.03ms.",
                        Description = "انغمس في عوالم الألعاب مع شاشة سامسونج Odyssey OLED G9 مقاس 49 بوصة بنسبة عرض إلى ارتفاع 32:9. تتميز بتقنية OLED المتقدمة مع ألوان زاهية وتباين مذهل ومعالج Neo Quantum Pro الفائق.",
                        BasePrice = 1599m,
                        CostPrice = 1180m,
                        CompareAtPrice = 1799m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 11,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80", true, "شاشة ألعاب منحنية عريضة"),
                        ("https://images.unsplash.com/photo-1547082299-de196ea013d6?w=800&auto=format&fit=crop&q=80", false, "مكتب جيمنج مع شاشة احترافية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    15
                ),

                // 20. Miss Dior Eau de Parfum
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = beautyCat?.Id,
                        BrandId = diorBrand?.Id,
                        Name = "عطر ميس ديور أو دو بارفان النسائي الفاخر (Miss Dior EDP)",
                        Slug = "miss-dior-eau-de-parfum",
                        Sku = "DIOR-MISSDIOR-EDP",
                        ShortDescription = "عطر نسائي زهري ساحر يفيض بنفحات الورد الجوري والفاوانيا والسوسن مع لمسات الفانيليا الرقيقة.",
                        Description = "عطر Miss Dior Eau de Parfum هو باقة زهرية مفعمة بالحيوية والحياة. تتماوج روائح زهور سنتيفوليا مع الورد الدمشقي والبرغموت المنعش لتعبر عن الأنوثة الراقية والأناقة الفرنسية الخالدة.",
                        BasePrice = 135m,
                        CostPrice = 80m,
                        CompareAtPrice = 155m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 33,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر ميس ديور الفاخرة"),
                        ("https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&auto=format&fit=crop&q=80", false, "عطر نسائي أنيق")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("حجم 50 مل", "DIOR-MD-50ML", 135m, 155m, new[] { ("الحجم", "VOLUME", "50 مل") }),
                        ("حجم 100 مل", "DIOR-MD-100ML", 175m, 195m, new[] { ("الحجم", "VOLUME", "100 مل") })
                    },
                    60
                ),

                // 21. Calvin Klein CK One
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = beautyCat?.Id,
                        BrandId = calvinBrand?.Id,
                        Name = "عطر كالفن كلاين سي كيه ون للجنسين (Calvin Klein CK One EDT)",
                        Slug = "calvin-klein-ck-one-edt",
                        Sku = "CK-ONE-EDT",
                        ShortDescription = "العطر الأيقوني المنعش للجنسين بنفحات الشاي الأخضر والبابايا والبرغموت والمسك.",
                        Description = "عطر CK One من كالفن كلاين يجسد روح الحرية والنقاء بتوليفة فريدة تناسب الرجال والنساء على حد سواء. افتتاحية منعشة من الحمضيات والبرغموت والهيل، وقلب عطري من الياسمين والبنفسج والورد، وقاعدة خشبية دافئة من العنبر والمسك.",
                        BasePrice = 65m,
                        CostPrice = 35m,
                        CompareAtPrice = 85m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 28,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر كالفن كلاين سي كيه ون"),
                        ("https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&auto=format&fit=crop&q=80", false, "عطور كالفن كلاين المنعشة")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("حجم 100 مل", "CK-ONE-100ML", 65m, 85m, new[] { ("الحجم", "VOLUME", "100 مل") }),
                        ("حجم 200 مل", "CK-ONE-200ML", 95m, 115m, new[] { ("الحجم", "VOLUME", "200 مل") })
                    },
                    75
                ),

                // 22. L'Oréal Revitalift Hyaluronic Acid Serum
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = beautyCat?.Id ?? personalCareCat?.Id,
                        BrandId = lorealBrand?.Id,
                        Name = "سيروم لوريال ريفايتلاليفت بحمض الهيالورونيك 1.5% (L'Oréal Revitalift)",
                        Slug = "loreal-revitalift-hyaluronic-acid-serum",
                        Sku = "LOR-REVIT-HA-30",
                        ShortDescription = "سيروم مكثف لترطيب البشرة واستعادة امتلائها وتقليل التجاعيد بنسبة 1.5% حمض الهيالورونيك النقي.",
                        Description = "يعد سيروم لوريال ريفايتلاليفت بحمض الهيالورونيك النقي الحل المثالي لبشرة نضرة ومشدودة. تركيبة خفيفة وسريعة الامتصاص تتغلغل بعمق في خلايا البشرة لترطيبها فورياً وتقليل الخطوط الدقيقة والتجاعيد خلال أسبوعين فقط.",
                        BasePrice = 29m,
                        CostPrice = 14m,
                        CompareAtPrice = 39m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 54,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&auto=format&fit=crop&q=80", true, "سيروم العناية بالبشرة لوريال"),
                        ("https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&auto=format&fit=crop&q=80", false, "مستحضرات العناية بالبشرة")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("سعة 30 مل", "LOR-HA-30ML", 29m, 39m, new[] { ("الحجم", "VOLUME", "30 مل") }),
                        ("سعة 50 مل", "LOR-HA-50ML", 42m, 55m, new[] { ("الحجم", "VOLUME", "50 مل") })
                    },
                    110
                ),

                // 23. Zara Slim Fit Oxford Shirt
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = fashionCat?.Id,
                        BrandId = zaraBrand?.Id,
                        Name = "قميص كلاسيكي قطن أكسفورد من زارا (Zara Oxford Cotton Shirt)",
                        Slug = "zara-oxford-cotton-shirt",
                        Sku = "ZRA-SHIRT-OXF",
                        ShortDescription = "قميص رجالي أنيق بقصة سليم فيت مصنوع من القطن الطبيعي 100% مناسب للعمل والمناسبات.",
                        Description = "يجمع قميص زارا أكسفورد بين الطراز الكلاسيكي المتقن والراحة الفائقة. منسوج من خيوط القطن الطبيعي عالية الجودة مع ياقة بأزرار وأكمام طويلة قابلة للطي ليكون خيارك اليومي المفضل.",
                        BasePrice = 45m,
                        CostPrice = 22m,
                        CompareAtPrice = 59m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.6m,
                        ReviewCount = 18,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80", true, "قميص أكسفورد أبيض أنيق"),
                        ("https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&auto=format&fit=crop&q=80", false, "قميص رجالي بأزرار")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أبيض - مقاس M", "ZRA-SHIRT-WHT-M", 45m, 59m, new[] { ("اللون", "COLOR", "أبيض"), ("المقاس", "SIZE", "M") }),
                        ("أبيض - مقاس L", "ZRA-SHIRT-WHT-L", 45m, 59m, new[] { ("اللون", "COLOR", "أبيض"), ("المقاس", "SIZE", "L") }),
                        ("أزرق سماوي - مقاس M", "ZRA-SHIRT-BLU-M", 45m, 59m, new[] { ("اللون", "COLOR", "أزرق سماوي"), ("المقاس", "SIZE", "M") }),
                        ("أزرق سماوي - مقاس L", "ZRA-SHIRT-BLU-L", 45m, 59m, new[] { ("اللون", "COLOR", "أزرق سماوي"), ("المقاس", "SIZE", "L") })
                    },
                    120
                ),

                // 24. Nike Club Fleece Hoodie
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = fashionCat?.Id ?? sportsCat?.Id,
                        BrandId = nikeBrand?.Id,
                        Name = "هودي رياضي نايكي كلوب فليس بغطاء رأس (Nike Club Fleece Hoodie)",
                        Slug = "nike-club-fleece-hoodie",
                        Sku = "NKE-HOODIE-CF",
                        ShortDescription = "هودي دافئ ومريح بصوف ناعم مصقول وقصة كلاسيكية مميزة مع شعار نايكي المطرز.",
                        Description = "يعد هودي Nike Sportswear Club Fleece قطعة أساسية في خزانة ملابسك، حيث يجمع بين الأناقة اليومية والراحة الفائقة بفضل نسيج الصوف الناعم المصقول من الداخل وجيب الكنغر الأمامي الواسع.",
                        BasePrice = 65m,
                        CostPrice = 32m,
                        CompareAtPrice = 80m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 29,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80", true, "هودي نايكي رياضي أسود"),
                        ("https://images.unsplash.com/photo-1578587018452-892bacefd3f2?w=800&auto=format&fit=crop&q=80", false, "تفاصيل هودي نايكي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود - مقاس M", "NKE-HOOD-BLK-M", 65m, 80m, new[] { ("اللون", "COLOR", "أسود"), ("المقاس", "SIZE", "M") }),
                        ("أسود - مقاس L", "NKE-HOOD-BLK-L", 65m, 80m, new[] { ("اللون", "COLOR", "أسود"), ("المقاس", "SIZE", "L") }),
                        ("رمادي هيذر - مقاس M", "NKE-HOOD-GRY-M", 65m, 80m, new[] { ("اللون", "COLOR", "رمادي هيذر"), ("المقاس", "SIZE", "M") }),
                        ("رمادي هيذر - مقاس L", "NKE-HOOD-GRY-L", 65m, 80m, new[] { ("اللون", "COLOR", "رمادي هيذر"), ("المقاس", "SIZE", "L") })
                    },
                    95
                ),

                // 25. Puma RS-X Efekt Lifestyle Shoes
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = shoesCat?.Id ?? sportsCat?.Id,
                        BrandId = pumaBrand?.Id,
                        Name = "حذاء بوما آر إس-إكس إفكت الرياضي (Puma RS-X Efekt)",
                        Slug = "puma-rs-x-efekt-sneakers",
                        Sku = "PMA-RSX-EFEKT",
                        ShortDescription = "سنيكرز عصري جريء بنظام التوسيد الشهير Running System وألوان متعددة لافتة للأنظار.",
                        Description = "يعيد حذاء Puma RS-X تعريف أسلوب الشارع العصري بتصميم مستقبلي وطبقات متعددة من الشبك والجلد الصناعي، مع نعل سميك ومريح يوفر توسيداً مثالياً للقدم طوال ساعات اليوم.",
                        BasePrice = 115m,
                        CostPrice = 60m,
                        CompareAtPrice = 135m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 21,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=800&auto=format&fit=crop&q=80", true, "حذاء بوما آر إس-إكس"),
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", false, "تفاصيل حذاء بوما")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("مقاس 41", "PMA-RSX-41", 115m, 135m, new[] { ("المقاس", "SIZE", "41") }),
                        ("مقاس 42", "PMA-RSX-42", 115m, 135m, new[] { ("المقاس", "SIZE", "42") }),
                        ("مقاس 43", "PMA-RSX-43", 115m, 135m, new[] { ("المقاس", "SIZE", "43") }),
                        ("مقاس 44", "PMA-RSX-44", 115m, 135m, new[] { ("المقاس", "SIZE", "44") })
                    },
                    70
                ),

                // 26. Casio G-Shock GA-2100 "CasiOak"
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = watchesCat?.Id,
                        BrandId = casioBrand?.Id,
                        Name = "ساعة كاسيو جي شوك المقاومة للصدمات GA-2100 (Casio G-Shock)",
                        Slug = "casio-g-shock-ga-2100",
                        Sku = "CSO-GSHOCK-GA2100",
                        ShortDescription = "ساعة جي شوك الأيقونية بهيكل الكربون القوي وتصميم ثماني الأضلاع نحيف ومقاومة للماء 200 متر.",
                        Description = "تتميز ساعة Casio G-Shock GA-2100 الشهيرة بلقب 'كاسيو أوك' بهيكل نحيف متين مدعم بألياف الكربون Carbon Core Guard، مع شاشة تناظرية ورقمية مزدوجة وإضاءة LED مزدوجة ومقاومة فائقة للصدمات والمياه حتى عمق 200 متر.",
                        BasePrice = 110m,
                        CostPrice = 65m,
                        CompareAtPrice = 130m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 37,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80", true, "ساعة كاسيو جي شوك سوداء"),
                        ("https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", false, "ساعة كاسيو بالمعصم")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود بالكامل (All Black)", "CSO-GA2100-BLK", 110m, 130m, new[] { ("اللون", "COLOR", "أسود بالكامل") }),
                        ("رمادي تكتيكي (Stealth Gray)", "CSO-GA2100-GRY", 110m, 130m, new[] { ("اللون", "COLOR", "رمادي تكتيكي") }),
                        ("كحلي داكن (Navy Blue)", "CSO-GA2100-NVY", 110m, 130m, new[] { ("اللون", "COLOR", "كحلي داكن") })
                    },
                    55
                ),

                // 27. Nespresso Vertuo Pop Coffee Machine
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = homeCat?.Id,
                        BrandId = nespressoBrand?.Id,
                        Name = "ماكينة قهوة نسبريسو فيرتو بوب الذكية (Nespresso Vertuo Pop)",
                        Slug = "nespresso-vertuo-pop-machine",
                        Sku = "NSP-VERTUO-POP",
                        ShortDescription = "ماكينة قهوة أنيقة ومضغوطة بتقنية الطرد المركزي Centrifusion لتحضير 4 أحجام مختلفة من الأكواب.",
                        Description = "أضف لمسة من البهجة لمطبخك مع ماكينة Nespresso Vertuo Pop. تقرأ الماكينة الرمز الشريطي لكل كبسولة لتعديل معايير الاستخلاص تلقائياً وتقديم فنجان قهوة كريمي غني برغوة لا تقاوم بلمسة زر واحدة.",
                        BasePrice = 129m,
                        CostPrice = 75m,
                        CompareAtPrice = 159m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 26,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", true, "ماكينة قهوة نسبريسو فيرتو بوب"),
                        ("https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=800&auto=format&fit=crop&q=80", false, "فنجان قهوة نسبريسو برغوة كريمية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود كلاسيكي (Licorice Black)", "NSP-VPOP-BLK", 129m, 159m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أبيض جوز الهند (Coconut White)", "NSP-VPOP-WHT", 129m, 159m, new[] { ("اللون", "COLOR", "أبيض جوز الهند") }),
                        ("أحمر ناري (Spicy Red)", "NSP-VPOP-RED", 129m, 159m, new[] { ("اللون", "COLOR", "أحمر ناري") })
                    },
                    40
                ),

                // 28. Philips Airfryer XXL Smart Sensing
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = homeCat?.Id ?? smartHomeCat?.Id,
                        BrandId = philipsBrand?.Id,
                        Name = "قلاية فيليبس الهوائية الذكية XXL سعة 7.3 لتر (Philips Airfryer XXL)",
                        Slug = "philips-airfryer-xxl-smart",
                        Sku = "PHL-AIRFRYER-XXL",
                        ShortDescription = "قلاية هوائية عائلية بتقنية إزالة الدهون Rapid Air وبرامج طهي ذكية بلمسة واحدة.",
                        Description = "حضّر أشهى الوجبات الصحية المقرمشة لعائلتك مع قلاية Philips Airfryer XXL. تطهو الطعام بدهون أقل بنسبة تصل إلى 90% مع تقنية استشعار ذكية تضبط الوقت ودرجة الحرارة تلقائياً للحصول على نتائج مثالية في كل مرة.",
                        BasePrice = 299m,
                        CostPrice = 185m,
                        CompareAtPrice = 349m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 39,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=800&auto=format&fit=crop&q=80", true, "قلاية هوائية ذكية للمطبخ"),
                        ("https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=800&auto=format&fit=crop&q=80", false, "أجهزة المطبخ الحديثة")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    35
                ),

                // 29. Xiaomi Robot Vacuum S10
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = smartHomeCat?.Id ?? homeCat?.Id,
                        BrandId = xiaomiBrand?.Id,
                        Name = "مكنسة روبوتية ذكية شاومي ممسحة ومكنسة S10 (Xiaomi Robot Vacuum S10)",
                        Slug = "xiaomi-robot-vacuum-s10",
                        Sku = "XMI-ROBOT-S10",
                        ShortDescription = "مكنسة ذكية بنظام الملاحة الليزرية LDS وقوة شفط 4000 باسكال ومسح ذكي متعرج.",
                        Description = "تحكم بنظافة منزلك عن بُعد مع مكنسة Xiaomi Robot Vacuum S10. مزودة برادار ليزري 360 درجة لرسم خرائط دقيقة لمنزلك، مع خزان مياه ذكي لحماية الأرضيات وقوة شفط فائقة تجمع أدق الأتربة وشعر الحيوانات الأليفة.",
                        BasePrice = 249m,
                        CostPrice = 160m,
                        CompareAtPrice = 299m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.7m,
                        ReviewCount = 23,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1518640467707-6811f4a6ab73?w=800&auto=format&fit=crop&q=80", true, "مكنسة روبوتية ذكية شاومي"),
                        ("https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=800&auto=format&fit=crop&q=80", false, "أجهزة المنزل الذكي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    28
                ),

                // 30. Anker MagGo Magnetic Power Bank 10000mAh
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = electronicsCat?.Id ?? smartphonesCat?.Id,
                        BrandId = ankerBrand?.Id,
                        Name = "شاحن باور بانك مغناطيسي لاسلكي أنكر ماج جو 10000 مللي أمبير (Anker MagGo)",
                        Slug = "anker-maggo-power-bank-10000mah",
                        Sku = "ANK-MAGGO-10K",
                        ShortDescription = "شاحن لاسلكي مغناطيسي سريع بقوة 15 واط معتمد من Qi2 وشاشة رقمية ذكية توضح نسبة الشحن.",
                        Description = "اشحن هاتف الآيفون أو الأجهزة المتوافقة بسرعة فائقة مع شاحن Anker MagGo اللاسلكي المغناطيسي. بفضل تقنية Qi2 المعتمدة والشاشة الذكية التي تبين نسبة البطارية والوقت المتبقي للشحن بدقة متناهية.",
                        BasePrice = 79m,
                        CostPrice = 42m,
                        CompareAtPrice = 99m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = true,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 41,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1609592424364-c289069d2f2d?w=800&auto=format&fit=crop&q=80", true, "شاحن باور بانك لاسلكي أنكر"),
                        ("https://images.unsplash.com/photo-1583863788434-e58a36330cf0?w=800&auto=format&fit=crop&q=80", false, "ملحقات الشحن السريع")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("أسود كلاسيكي", "ANK-MAGGO-BLK", 79m, 99m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أبيض لؤلؤي", "ANK-MAGGO-WHT", 79m, 99m, new[] { ("اللون", "COLOR", "أبيض لؤلؤي") }),
                        ("بنفسجي هادئ", "ANK-MAGGO-PUR", 79m, 99m, new[] { ("اللون", "COLOR", "بنفسجي هادئ") })
                    },
                    120
                ),

                // 31. Ray-Ban Aviator Classic Sunglasses
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = eyewearCat?.Id ?? watchesCat?.Id,
                        BrandId = raybanBrand?.Id,
                        Name = "نظارة شمسية ريبان أفياتور الكلاسيكية الأصلية (Ray-Ban Aviator Classic)",
                        Slug = "ray-ban-aviator-classic-sunglasses",
                        Sku = "RB-AVIATOR-3025",
                        ShortDescription = "النظارة الشمسية الأكثر شهرة في التاريخ بإطار معدني ذهبي وعدسات G-15 الأيقونية لحماية 100% من UV.",
                        Description = "صُممت نظارات Ray-Ban Aviator Classic في الأصل للطيارين الأمريكيين عام 1937، وتعتبر اليوم رمزاً للأناقة والجودة التي لا تبطل موضتها. توفر عدسات G-15 وضوحاً بصرياً فائقاً وراحة تامة للعين مع حماية تامة من الأشعة فوق البنفسجية.",
                        BasePrice = 165m,
                        CostPrice = 90m,
                        CompareAtPrice = 185m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.9m,
                        ReviewCount = 30,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=800&auto=format&fit=crop&q=80", true, "نظارة شمسية ريبان أفياتور"),
                        ("https://images.unsplash.com/photo-1508296695146-257a814070b4?w=800&auto=format&fit=crop&q=80", false, "تفاصيل عدسات ريبان")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>
                    {
                        ("إطار ذهبي - عدسات خضراء G-15", "RB-3025-GLD-G15", 165m, 185m, new[] { ("اللون", "COLOR", "إطار ذهبي / أخضر G-15") }),
                        ("إطار أسود - عدسات مستقطبة Polarized", "RB-3025-BLK-POL", 195m, 215m, new[] { ("اللون", "COLOR", "إطار أسود / مستقطب") })
                    },
                    50
                ),

                // 32. Philips Series 9000 Wet & Dry Shaver
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = personalCareCat?.Id ?? beautyCat?.Id,
                        BrandId = philipsBrand?.Id,
                        Name = "ماكينة حلاقة كهربائية ذكية فيليبس سلسلة 9000 (Philips Series 9000)",
                        Slug = "philips-series-9000-shaver",
                        Sku = "PHL-SHAVER-S9000",
                        ShortDescription = "ماكينة الحلاقة الأكثر تطوراً بتقنية الذكاء الاصطناعي SkinIQ ورؤوس مرنة تدور في 360 درجة.",
                        Description = "توفر ماكينة الحلاقة Philips Series 9000 حلاقة فائقة النعومة مع حماية قصوى للبشرة. تستشعر كثافة اللحية 500 مرة في الثانية وتتكيف تلقائياً لمنحك حلاقة مريحة وسريعة سواء كنت تفضل الحلاقة الجافة أو الرطبة بالجل.",
                        BasePrice = 229m,
                        CostPrice = 140m,
                        CompareAtPrice = 269m,
                        CurrencyCode = "USD",
                        IsActive = true,
                        IsFeatured = false,
                        TrackInventory = true,
                        AverageRating = 4.8m,
                        ReviewCount = 25,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1621607512214-68297480165e?w=800&auto=format&fit=crop&q=80", true, "ماكينة حلاقة كهربائية فيليبس"),
                        ("https://images.unsplash.com/photo-1503951914875-452162b0f3f1?w=800&auto=format&fit=crop&q=80", false, "أدوات العناية الشخصية للرجال")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                )
            };

            foreach (var item in productsToSeed)
            {
                var existingProduct = await db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.VariantAttributes)
                    .Include(p => p.InventoryItems)
                    .FirstOrDefaultAsync(p => p.Slug == item.product.Slug);

                if (existingProduct == null)
                {
                    var product = item.product;
                    await db.Products.AddAsync(product);
                    await db.SaveChangesAsync();

                    // Seed Images
                    int sortOrder = 0;
                    foreach (var img in item.images)
                    {
                        var prodImage = new ProductImage
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Url = img.url,
                            IsPrimary = img.isPrimary,
                            AltText = img.alt,
                            SortOrder = sortOrder++,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await db.ProductImages.AddAsync(prodImage);
                    }

                    // Seed Variants
                    foreach (var v in item.variants)
                    {
                        var variant = new ProductVariant
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Name = v.name,
                            Sku = v.sku,
                            Price = v.price,
                            CostPrice = product.CostPrice,
                            CompareAtPrice = v.compareAt,
                            IsActive = true,
                            TrackInventory = true,
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        await db.ProductVariants.AddAsync(variant);

                        // Variant option values — these drive the storefront option matrix.
                        await SeedVariantAttributesAsync(db, variant.Id, v.options, attributeCache);

                        // Variant inventory
                        var varInv = new InventoryItem(product.Id, mainWarehouse.Id, item.stock / Math.Max(item.variants.Count, 1), variant.Id);
                        await db.InventoryItems.AddAsync(varInv);
                    }

                    // Product-level inventory
                    var prodInv = new InventoryItem(product.Id, mainWarehouse.Id, item.stock);
                    await db.InventoryItems.AddAsync(prodInv);

                    await db.SaveChangesAsync();
                }
                else
                {
                    // Update details and make sure images are populated
                    existingProduct.Name = item.product.Name;
                    existingProduct.Description = item.product.Description;
                    existingProduct.ShortDescription = item.product.ShortDescription;
                    existingProduct.BasePrice = item.product.BasePrice;
                    existingProduct.CompareAtPrice = item.product.CompareAtPrice;
                    existingProduct.CostPrice = item.product.CostPrice;
                    existingProduct.IsFeatured = item.product.IsFeatured;
                    existingProduct.IsActive = item.product.IsActive;
                    existingProduct.CategoryId = item.product.CategoryId;
                    existingProduct.BrandId = item.product.BrandId;
                    existingProduct.AverageRating = item.product.AverageRating;
                    existingProduct.ReviewCount = item.product.ReviewCount;

                    if (!existingProduct.Images.Any())
                    {
                        int sortOrder = 0;
                        foreach (var img in item.images)
                        {
                            var prodImage = new ProductImage
                            {
                                Id = Guid.NewGuid(),
                                ProductId = existingProduct.Id,
                                Url = img.url,
                                IsPrimary = img.isPrimary,
                                AltText = img.alt,
                                SortOrder = sortOrder++,
                                CreatedAt = DateTimeOffset.UtcNow
                            };
                            await db.ProductImages.AddAsync(prodImage);
                        }
                    }

                    foreach (var v in item.variants)
                    {
                        var variant = existingProduct.Variants.FirstOrDefault(x => x.Sku == v.sku);
                        if (variant == null)
                        {
                            variant = new ProductVariant
                            {
                                Id = Guid.NewGuid(),
                                ProductId = existingProduct.Id,
                                Name = v.name,
                                Sku = v.sku,
                                Price = v.price,
                                CostPrice = existingProduct.CostPrice,
                                CompareAtPrice = v.compareAt,
                                IsActive = true,
                                TrackInventory = true,
                                CreatedAt = DateTimeOffset.UtcNow,
                                UpdatedAt = DateTimeOffset.UtcNow
                            };
                            await db.ProductVariants.AddAsync(variant);
                        }

                        if (variant.VariantAttributes == null || variant.VariantAttributes.Count == 0)
                        {
                            await SeedVariantAttributesAsync(db, variant.Id, v.options, attributeCache);
                        }
                    }

                    if (item.variants.Count > 0)
                    {
                        existingProduct.AttributesJson = null;
                    }

                    if (!existingProduct.InventoryItems.Any())
                    {
                        var prodInv = new InventoryItem(existingProduct.Id, mainWarehouse.Id, item.stock);
                        await db.InventoryItems.AddAsync(prodInv);
                    }

                    await db.SaveChangesAsync();
                }

                var productIdToIndex = existingProduct != null ? existingProduct.Id : item.product.Id;
                await _searchService.IndexProductAsync(productIdToIndex);
            }

            _logger.LogInformation("Seeded and synchronized {Count} rich products, variants and variant options", productsToSeed.Count);
        }

        private static async Task SeedVariantAttributesAsync(
            ApplicationDbContext db,
            Guid variantId,
            (string attribute, string code, string value)[] options,
            Dictionary<string, ProductAttribute> attributeCache)
        {
            if (options == null || options.Length == 0) return;

            foreach (var (attributeName, code, value) in options)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;

                if (!attributeCache.TryGetValue(code, out var attribute))
                {
                    attribute = await db.ProductAttributes.FirstOrDefaultAsync(a => a.Code == code)
                                ?? await db.ProductAttributes.FirstOrDefaultAsync(a => a.Name == attributeName);

                    if (attribute == null)
                    {
                        attribute = new ProductAttribute
                        {
                            Id = Guid.NewGuid(),
                            Name = attributeName,
                            Code = code,
                            DisplayType = "Select",
                            IsFilterable = true,
                            IsVariant = true,
                            IsRequired = true,
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        await db.ProductAttributes.AddAsync(attribute);
                        await db.SaveChangesAsync();
                    }
                    else if (string.IsNullOrWhiteSpace(attribute.Code))
                    {
                        attribute.Code = code;
                        attribute.DisplayType = string.IsNullOrWhiteSpace(attribute.DisplayType) ? "Select" : attribute.DisplayType;
                        attribute.IsVariant = true;
                    }

                    attributeCache[code] = attribute;
                }

                var alreadyLinked = await db.ProductVariantAttributes
                    .AnyAsync(va => va.ProductVariantId == variantId && va.ProductAttributeId == attribute.Id);
                if (alreadyLinked) continue;

                await db.ProductVariantAttributes.AddAsync(new ProductVariantAttribute
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    ProductAttributeId = attribute.Id,
                    Value = value,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        private async Task SeedProductReviewsAsync(ApplicationDbContext db, UserManager<ApplicationUser>? userManager)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "e2e-customer@example.com")
                       ?? await db.Users.FirstOrDefaultAsync();
            if (user == null) return;

            var iphone = await db.Products.FirstOrDefaultAsync(p => p.Slug == "iphone-15-pro-max");
            var sony = await db.Products.FirstOrDefaultAsync(p => p.Slug == "sony-wh-1000xm5-headphones");
            var sauvage = await db.Products.FirstOrDefaultAsync(p => p.Slug == "dior-sauvage-edp");
            var nike = await db.Products.FirstOrDefaultAsync(p => p.Slug == "nike-air-max-plus-sneakers");
            var s24 = await db.Products.FirstOrDefaultAsync(p => p.Slug == "samsung-galaxy-s24-ultra");
            var dell = await db.Products.FirstOrDefaultAsync(p => p.Slug == "dell-xps-15-oled-laptop");

            var reviews = new List<ProductReview>();

            if (iphone != null && !await db.ProductReviews.AnyAsync(r => r.ProductId == iphone.Id && r.UserId == user.Id))
            {
                reviews.Add(new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = iphone.Id,
                    UserId = user.Id,
                    Rating = 5,
                    Title = "هاتف خارق وتجربة تصوير لا تُضاهى",
                    Comment = "الهاتف ممتاز جداً، خفة وزن التيتانيوم واضحة مقارنة بالإصدارات السابقة، والبطارية تدوم يوماً كاملاً بالراحة والكاميرا 5X احترافية للغاية.",
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-15),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-15)
                });
            }

            if (sony != null && !await db.ProductReviews.AnyAsync(r => r.ProductId == sony.Id && r.UserId == user.Id))
            {
                reviews.Add(new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = sony.Id,
                    UserId = user.Id,
                    Rating = 5,
                    Title = "أفضل سماعة عازلة للضوضاء جربتها",
                    Comment = "عزل الصوت أسطوري وراحة تامة على الأذن حتى مع ارتدائها لساعات طويلة في العمل والمكالمات. نقاء الصوت مذهل.",
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10)
                });
            }

            if (sauvage != null && !await db.ProductReviews.AnyAsync(r => r.ProductId == sauvage.Id && r.UserId == user.Id))
            {
                reviews.Add(new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = sauvage.Id,
                    UserId = user.Id,
                    Rating = 5,
                    Title = "عطر فخم وثبات يدوم لأيام",
                    Comment = "عطر أصلي 100%، فوحان قوي وثبات ممتاز يستمر طوال اليوم والكل يسألني عن رائحته. تغليف أنيق وتوصيل سريع.",
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-8)
                });
            }

            if (nike != null && !await db.ProductReviews.AnyAsync(r => r.ProductId == nike.Id && r.UserId == user.Id))
            {
                reviews.Add(new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = nike.Id,
                    UserId = user.Id,
                    Rating = 5,
                    Title = "حذاء مريح جداً وتصميم جذاب",
                    Comment = "المقاس مضبوط تماماً ومريح جداً للمشي اليومي والتمارين في الجيم. جودة الخامات ممتازة وأصلية.",
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5)
                });
            }

            if (s24 != null && !await db.ProductReviews.AnyAsync(r => r.ProductId == s24.Id && r.UserId == user.Id))
            {
                reviews.Add(new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = s24.Id,
                    UserId = user.Id,
                    Rating = 5,
                    Title = "شاشة مذهلة وميزات ذكاء اصطناعي ثورية",
                    Comment = "الشاشة المسطحة خرافية وميزات Galaxy AI مفيدة جداً في الترجمة وتعديل الصور. القلم دقيق وسريع الاستجابة.",
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3)
                });
            }

            if (reviews.Count > 0)
            {
                await db.ProductReviews.AddRangeAsync(reviews);
                await db.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} verified customer product reviews", reviews.Count);
            }
        }

        private async Task SeedPromotionsAsync(ApplicationDbContext db)
        {
            var seedPromotions = new List<Promotion>
            {
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "خصم الصيف الكبير 15% (Summer Mega Sale)",
                    Description = "خصم 15% على جميع المنتجات المؤهلة لفترة محدودة",
                    Type = "percentage",
                    RulesJson = "{\"discountPercentage\": 15}",
                    Priority = 10,
                    AllowCombine = true,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(5),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "عرض وفر 50 شيكل فوري (Save 50 ILS)",
                    Description = "خصم فوري بقيمة 50 شيكل عند التسوق",
                    Type = "fixed_amount",
                    RulesJson = "{\"discountAmount\": 50}",
                    Priority = 5,
                    AllowCombine = true,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(5),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "عرض اشتري 2 واحصل على 1 مجاناً (Buy 2 Get 1 Free)",
                    Description = "اشتري قطعتين واحصل على الثالثة مجاناً",
                    Type = "buy_x_get_y",
                    RulesJson = "{\"buyQuantity\": 2, \"getQuantity\": 1, \"discountPercentage\": 100}",
                    Priority = 15,
                    AllowCombine = false,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(5),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "خصومات السلة المتدرجة (Tiered Cart Discount)",
                    Description = "تسوق أكثر ووفر أكثر: خصم يصل حتى 150 شيكل حسب قيمة مشترياتك في السلة",
                    Type = "tiered_discount",
                    RulesJson = "{\"tiers\": [{\"minSpend\": 250, \"discount\": 25, \"discountType\": \"fixed_amount\"}, {\"minSpend\": 500, \"discount\": 60, \"discountType\": \"fixed_amount\"}, {\"minSpend\": 1000, \"discount\": 150, \"discountType\": \"fixed_amount\"}]}",
                    Priority = 20,
                    AllowCombine = true,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(5),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "عرض باقة الإلكترونيات والهواتف (Tech Festival 20%)",
                    Description = "خصم حصري 20% على أحدث الإلكترونيات والهواتف الذكية",
                    Type = "percentage",
                    RulesJson = "{\"discountPercentage\": 20}",
                    Priority = 12,
                    AllowCombine = true,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(5),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            foreach (var promo in seedPromotions)
            {
                var existing = await db.Promotions.FirstOrDefaultAsync(p => p.Name == promo.Name);
                if (existing == null)
                {
                    await db.Promotions.AddAsync(promo);
                }
                else
                {
                    existing.Description = promo.Description;
                    existing.Type = promo.Type;
                    existing.RulesJson = promo.RulesJson;
                    existing.Priority = promo.Priority;
                    existing.IsActive = promo.IsActive;
                    existing.StartAt = promo.StartAt;
                    existing.EndAt = promo.EndAt;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded promotions and discount rules");
        }

        private async Task SeedCouponsAsync(ApplicationDbContext db)
        {
            var seedCoupons = new List<Coupon>
            {
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "SAVE20",
                    Description = "خصم 20% على جميع المشتريات",
                    Type = "percentage",
                    Value = 20m,
                    MaxDiscountAmount = 500m,
                    MinOrderAmount = 0m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "SOFAN10",
                    Description = "خصم فوري بقيمة 10 شيكل",
                    Type = "fixed_amount",
                    Value = 10m,
                    MinOrderAmount = 0m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "WELCOME15",
                    Description = "خصم 15% للعملاء الجدد على أول طلب",
                    Type = "percentage",
                    Value = 15m,
                    MaxDiscountAmount = 200m,
                    MinOrderAmount = 50m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "MEGA50",
                    Description = "خصم 50 شيكل للطلبات بقيمة 250 شيكل فأكثر",
                    Type = "fixed_amount",
                    Value = 50m,
                    MinOrderAmount = 250m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "SUMMER30",
                    Description = "خصم 30% عروض الصيف الخاطفة",
                    Type = "percentage",
                    Value = 30m,
                    MaxDiscountAmount = 300m,
                    MinOrderAmount = 100m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "VIP100",
                    Description = "خصم 100 شيكل حصري لعملاء VIP على الطلبات فوق 500 شيكل",
                    Type = "fixed_amount",
                    Value = 100m,
                    MinOrderAmount = 500m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "FREESHIP",
                    Description = "قسيمة شحن مجاني للطلبات المؤهلة",
                    Type = "fixed_amount",
                    Value = 20m,
                    MinOrderAmount = 100m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new Coupon
                {
                    Id = Guid.NewGuid(),
                    Code = "EID2026",
                    Description = "خصم 25% بمناسبة الأعياد والمناسبات",
                    Type = "percentage",
                    Value = 25m,
                    MaxDiscountAmount = 400m,
                    MinOrderAmount = 150m,
                    IsActive = true,
                    StartAt = DateTimeOffset.UtcNow.AddDays(-30),
                    EndAt = DateTimeOffset.UtcNow.AddYears(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            };

            foreach (var coupon in seedCoupons)
            {
                var existing = await db.Coupons.FirstOrDefaultAsync(c => c.Code == coupon.Code);
                if (existing == null)
                {
                    await db.Coupons.AddAsync(coupon);
                }
                else
                {
                    existing.Description = coupon.Description;
                    existing.Type = coupon.Type;
                    existing.Value = coupon.Value;
                    existing.MaxDiscountAmount = coupon.MaxDiscountAmount;
                    existing.MinOrderAmount = coupon.MinOrderAmount;
                    existing.IsActive = coupon.IsActive;
                    existing.StartAt = coupon.StartAt;
                    existing.EndAt = coupon.EndAt;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded coupons");
        }

        private async Task SeedSampleOrdersAsync(ApplicationDbContext db)
        {
            if (await db.Orders.AnyAsync()) return;

            var iphone = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "iphone-15-pro-max");
            var appleWatch = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "apple-watch-series-9");
            var sony = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "sony-wh-1000xm5-headphones");
            var dell = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "dell-xps-15-oled-laptop");
            var backpack = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "smart-anti-theft-laptop-backpack");
            var nike = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "nike-air-max-plus-sneakers");
            var adidas = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "adidas-ultraboost-1-sneakers");
            var dumbbells = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "adjustable-dumbbell-set-24kg");
            var zara = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "zara-winter-puffer-jacket");
            var dior = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "dior-sauvage-edp");

            var orders = new List<Order>();

            // Order 1: iPhone + Apple Watch + Sony Headphones
            if (iphone != null && appleWatch != null && sony != null)
            {
                var o1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-001", CurrencyCode = "USD" };
                o1.AddItem(iphone.Id, iphone.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), iphone.Name, iphone.BasePrice, 1);
                o1.AddItem(appleWatch.Id, appleWatch.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), appleWatch.Name, appleWatch.BasePrice, 1);
                o1.AddItem(sony.Id, sony.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), sony.Name, sony.BasePrice, 1);
                orders.Add(o1);
            }

            // Order 2: iPhone + Apple Watch
            if (iphone != null && appleWatch != null)
            {
                var o2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-002", CurrencyCode = "USD" };
                o2.AddItem(iphone.Id, iphone.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), iphone.Name, iphone.BasePrice, 1);
                o2.AddItem(appleWatch.Id, appleWatch.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), appleWatch.Name, appleWatch.BasePrice, 1);
                orders.Add(o2);
            }

            // Order 3: Dell Laptop + Sony Headphones + Backpack
            if (dell != null && sony != null && backpack != null)
            {
                var o3 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-003", CurrencyCode = "USD" };
                o3.AddItem(dell.Id, dell.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), dell.Name, dell.BasePrice, 1);
                o3.AddItem(sony.Id, sony.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), sony.Name, sony.BasePrice, 1);
                o3.AddItem(backpack.Id, backpack.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), backpack.Name, backpack.BasePrice, 1);
                orders.Add(o3);
            }

            // Order 4: Nike Shoes + Adidas Shoes + Dumbbells
            if (nike != null && adidas != null && dumbbells != null)
            {
                var o4 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-004", CurrencyCode = "USD" };
                o4.AddItem(nike.Id, nike.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), nike.Name, nike.BasePrice, 1);
                o4.AddItem(adidas.Id, adidas.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), adidas.Name, adidas.BasePrice, 1);
                o4.AddItem(dumbbells.Id, dumbbells.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), dumbbells.Name, dumbbells.BasePrice, 1);
                orders.Add(o4);
            }

            // Order 5: Zara Jacket + Dior Perfume
            if (zara != null && dior != null)
            {
                var o5 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-005", CurrencyCode = "USD" };
                o5.AddItem(zara.Id, zara.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), zara.Name, zara.BasePrice, 1);
                o5.AddItem(dior.Id, dior.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), dior.Name, dior.BasePrice, 1);
                orders.Add(o5);
            }

            if (orders.Count > 0)
            {
                await db.Orders.AddRangeAsync(orders);
                await db.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} realistic sample orders for recommendation co-occurrence matrix", orders.Count);
            }
        }
    }
}
