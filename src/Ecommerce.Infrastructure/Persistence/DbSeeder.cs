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

            // ── Admin seed ────────────────────────────────────────────────────────────
            var adminEmail = "admin@sofan.local";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "مدير",
                    LastName = "النظام",
                    DisplayName = "مدير النظام",
                    EmailConfirmed = true,
                    IsEmailVerified = true,
                    IsActive = true,
                    PhoneNumber = "0599000000",
                    PhoneNumberConfirmed = true,
                    IsPhoneVerified = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                var adminResult = await userManager.CreateAsync(adminUser, "AdminPassword123!");
                if (adminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    _logger.LogInformation("Seeded Admin user: {Email}", adminEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to seed Admin user: {Errors}", string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure the existing admin record is healthy (active, verified, in Admin role)
                var needsUpdate = false;
                if (!existingAdmin.EmailConfirmed || !existingAdmin.IsEmailVerified || !existingAdmin.IsActive)
                {
                    existingAdmin.EmailConfirmed = true;
                    existingAdmin.IsEmailVerified = true;
                    existingAdmin.IsActive = true;
                    needsUpdate = true;
                }
                if (needsUpdate) await userManager.UpdateAsync(existingAdmin);

                if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
                    await userManager.AddToRoleAsync(existingAdmin, "Admin");
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
                    PrimaryButtonLink = "/categories/smartphones",
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
                    PrimaryButtonLink = "/categories/perfumes-fragrances",
                    SecondaryButtonText = "أحدث الأزياء",
                    SecondaryButtonLink = "/categories/clothing-fashion",
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
            // 1. Root / Parent Categories
            var rootCategories = new List<(string name, string slug, string desc, string img, int order)>
            {
                ("إلكترونيات", "electronics", "أحدث الأجهزة الذكية والإلكترونيات الاستهلاكية وملحقاتها من كبرى الشركات العالمية", "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=600&auto=format&fit=crop&q=80", 1),
                ("ملابس وأزياء", "clothing-fashion", "أرقى صيحات الموضة والأزياء الرجالية والنسائية العصرية لجميع الفصول", "https://images.unsplash.com/photo-1445205170230-053b83016050?w=600&auto=format&fit=crop&q=80", 2),
                ("أحذية وحقائب", "shoes-bags", "أحذية رياضية ورسمية وحقائب سفر وظهر أنيقة بجودة استثنائية", "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=600&auto=format&fit=crop&q=80", 3),
                ("المنزل والمطبخ", "home-kitchen", "أجهزة تحضير القهوة وأدوات المطبخ الذكية ومستلزمات البيت العصري", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=600&auto=format&fit=crop&q=80", 4),
                ("العطور والجمال", "beauty-perfumes", "أفخم العطور الفرنسية والعالمية الأصلية ومنتجات العناية بالبشرة", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&auto=format&fit=crop&q=80", 5),
                ("الساعات والإكسسوارات", "watches-accessories", "ساعات فاخرة ورقمية وذكية تناسب جميع الإطلالات اليومية والرسمية", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80", 6),
                ("الرياضة واللياقة", "sports-fitness", "معدات اللياقة البدنية والتمارين المنزلية والملابس الرياضية المتينة", "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&auto=format&fit=crop&q=80", 7),
                ("بقالة ومواد غذائية", "groceries", "كل ما يحتاجه مطبخك: خضار وفواكه طازجة، لحوم ودواجن، مواد أساسية ومشروبات، بأسعار الجملة وتوصيل سريع.", "https://cdn.dummyjson.com/product-images/groceries/strawberry/1.webp", 8),
                ("سيارات ودراجات نارية", "vehicles-motorcycles", "معرض السيارات والدراجات النارية: موديلات حديثة، سيارات عائلية ودراجات رياضية بمواصفات كاملة.", "https://cdn.dummyjson.com/product-images/vehicle/charger-sxt-rwd/1.webp", 9),
                ("العناية الشخصية والصحة", "personal-care", "ماكينات حلاقة وأجهزة تصفيف الشعر ومستحضرات العناية الشخصية المتقدمة", "https://images.unsplash.com/photo-1621607512214-68297480165e?w=600&auto=format&fit=crop&q=80", 10),
            };

            var rootCategoryMap = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rootCategories)
            {
                var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == r.slug);
                if (existing == null)
                {
                    existing = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = r.name,
                        Slug = r.slug,
                        Description = r.desc,
                        ImageUrl = r.img,
                        DisplayOrder = r.order,
                        IsActive = true,
                        IsFeatured = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await db.Categories.AddAsync(existing);
                }
                else
                {
                    existing.Name = r.name;
                    existing.Description = r.desc;
                    existing.ImageUrl = r.img;
                    existing.ParentCategoryId = null; // Ensure root
                    existing.DisplayOrder = r.order;
                    existing.IsActive = true;
                }
                rootCategoryMap[r.slug] = existing;
            }
            await db.SaveChangesAsync();

            // 2. Subcategories with explicit parent linking
            var subCategoryDefinitions = new List<(string parentSlug, string name, string slug, string desc, string img, int order)>
            {
                // Electronics subcategories
                ("electronics", "الهواتف الذكية", "smartphones", "أحدث الهواتف الذكية من آبل وسامسونج وشاومي مع كافة الإكسسوارات والشواحن الأصلية", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&auto=format&fit=crop&q=80", 1),
                ("electronics", "ملحقات الهواتف", "phone-accessories", "شواحن سريعة وبطاريات متنقلة وكوابل وحافظات أصلية", "https://images.unsplash.com/photo-1609592424364-c289069d2f2d?w=600&auto=format&fit=crop&q=80", 2),
                ("electronics", "حواسيب محمولة", "laptops", "أجهزة لابتوب للأعمال والتصميم والألعاب من أبل وديل وأسوس ولينوفو", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&auto=format&fit=crop&q=80", 3),
                ("electronics", "أجهزة لوحية", "tablets", "أجهزة آيباد وتابلت سامسونج للرسم والقراءة والعمل المتنقل مع دعم القلم الرقمي", "https://cdn.dummyjson.com/product-images/tablets/ipad-mini-2021-starlight/1.webp", 4),
                ("electronics", "الصوتيات والسماعات", "audio-headphones", "سماعات رأس لاسلكية ومكبرات صوت فائقة النقاء وعازلة للضوضاء", "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=600&auto=format&fit=crop&q=80", 5),
                ("electronics", "تلفزيونات وشاشات", "tv-displays", "شاشات تلفزيون ذكية وشاشات كمبيوتر احترافية بدقة 4K", "https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=600&auto=format&fit=crop&q=80", 6),
                ("electronics", "ألعاب الفيديو والترفيه", "gaming-consoles", "أجهزة ألعاب بلايستيشن وإكس بوكس وملحقات الجيمنج والشاشات الاحترافية", "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=600&auto=format&fit=crop&q=80", 7),
                ("electronics", "الأجهزة المنزلية الذكية", "smart-home", "مكانس روبوتية ومقالي هوائية وأجهزة ذكية لتسهيل حياتك اليومية", "https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=600&auto=format&fit=crop&q=80", 8),

                // Clothing & Fashion subcategories
                ("clothing-fashion", "ملابس رجالية", "mens-clothing", "قمصان وبناطيل وهوديز وجاكيتات رجالية راقية", "https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=600&auto=format&fit=crop&q=80", 1),
                ("clothing-fashion", "ملابس نسائية", "womens-clothing", "فساتين وتنانير وملابس نسائية أنيقة لجميع المناسبات", "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=600&auto=format&fit=crop&q=80", 2),

                // Shoes & Bags subcategories
                ("shoes-bags", "أحذية رجالية", "mens-shoes", "أحذية رياضية ورسمية للرجال من كبرى الماركات", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600&auto=format&fit=crop&q=80", 1),
                ("shoes-bags", "أحذية نسائية", "womens-shoes", "سنيكرز وأحذية كعب وأحذية نسائية مريحة وعصرية", "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=600&auto=format&fit=crop&q=80", 2),
                ("shoes-bags", "حقائب نسائية", "womens-bags", "حقائب يد وحقائب ظهر وكتف جلدية أنيقة", "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=600&auto=format&fit=crop&q=80", 3),

                // Home & Kitchen subcategories
                ("home-kitchen", "أدوات مطبخ", "kitchen-tools", "ماكينات قهوة ومقالي هوائية وأدوات طهي عصرية", "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=600&auto=format&fit=crop&q=80", 1),
                ("home-kitchen", "ديكور منزلي", "home-decor", "إضاءات ومزهريات ولوحات وتحف لتجميل منزلك", "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=600&auto=format&fit=crop&q=80", 2),
                ("home-kitchen", "أثاث", "furniture", "كراسي وطاولات ومفروشات منزلية ومكتبية مريحة", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=600&auto=format&fit=crop&q=80", 3),

                // Beauty & Perfumes subcategories
                ("beauty-perfumes", "عطور", "perfumes-fragrances", "أفخم العطور الشرقية والفرنسية الأصلية", "https://images.unsplash.com/photo-1541643600914-78b084683601?w=600&auto=format&fit=crop&q=80", 1),
                ("beauty-perfumes", "العناية بالبشرة", "skincare", "سيرومات وكريمات ترطيب وغسول وجه للبشرة النضرة", "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=600&auto=format&fit=crop&q=80", 2),
                ("beauty-perfumes", "مكياج", "makeup", "مستحضرات تجميل وماسكارا وأحمر شفاه من أرقى العلامات", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&auto=format&fit=crop&q=80", 3),

                // Watches & Accessories subcategories
                ("watches-accessories", "ساعات رجالية", "mens-watches", "ساعات كلاسيكية ورقمية وذكية للرجال", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80", 1),
                ("watches-accessories", "ساعات نسائية", "womens-watches", "ساعات نسائية فاخرة بتصاميم أنيقة وجذابة", "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=600&auto=format&fit=crop&q=80", 2),
                ("watches-accessories", "نظارات شمسية", "sunglasses", "نظارات شمسية أصلية بحماية كاملة من الأشعة فوق البنفسجية", "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=600&auto=format&fit=crop&q=80", 3),
                ("watches-accessories", "مجوهرات وأقراط", "jewellery", "سلاسل وأساور وخواتم مميزة", "https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=600&auto=format&fit=crop&q=80", 4),

                // Sports & Fitness subcategories
                ("sports-fitness", "مستلزمات رياضية", "sports-equipment", "أثقال وأدوات تمارين وملحقات الجيم واللياقة", "https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=600&auto=format&fit=crop&q=80", 1),

                // Groceries subcategories
                ("groceries", "خضار وفواكه ولحوم", "fresh-produce", "منتجات طازجة ومختارة بعناية يومياً", "https://cdn.dummyjson.com/product-images/groceries/strawberry/1.webp", 1),
                ("groceries", "مواد أساسية", "pantry-staples", "زيوت وأرز وسكر ومعكرونة ومواد تموينية", "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=600&auto=format&fit=crop&q=80", 2),
                ("groceries", "مشروبات", "beverages", "قهوة وشاي وعصائر ومشروبات منعشة", "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=600&auto=format&fit=crop&q=80", 3),

                // Vehicles & Motorcycles subcategories
                ("vehicles-motorcycles", "سيارات", "cars", "سيارات وسيارات رياضية وسيدان حديثة", "https://cdn.dummyjson.com/product-images/vehicle/charger-sxt-rwd/1.webp", 1),
                ("vehicles-motorcycles", "دراجات نارية", "motorcycles", "دراجات نارية رياضية وسكوترات قوية", "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=600&auto=format&fit=crop&q=80", 2),
            };

            foreach (var sub in subCategoryDefinitions)
            {
                if (!rootCategoryMap.TryGetValue(sub.parentSlug, out var parentCat)) continue;

                var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == sub.slug);
                if (existing == null)
                {
                    existing = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = sub.name,
                        Slug = sub.slug,
                        Description = sub.desc,
                        ImageUrl = sub.img,
                        ParentCategoryId = parentCat.Id,
                        DisplayOrder = sub.order,
                        IsActive = true,
                        IsFeatured = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await db.Categories.AddAsync(existing);
                }
                else
                {
                    existing.Name = sub.name;
                    existing.Description = sub.desc;
                    existing.ImageUrl = sub.img;
                    existing.ParentCategoryId = parentCat.Id;
                    existing.DisplayOrder = sub.order;
                    existing.IsActive = true;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded and linked parent categories and subcategories hierarchy");
        }

        private async Task SeedBrandsAsync(ApplicationDbContext db)
        {
            var seedBrands = new List<(string name, string slug, string desc, string img)>
            {
                ("آبل (Apple)", "apple", "الشركة الرائدة عالمياً في الابتكار التكنولوجي والأجهزة الذكية المتكاملة", "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=300&auto=format&fit=crop&q=80"),
                ("سامسونج (Samsung)", "samsung", "تقنيات متطورة وشاشات مذهلة وأجهزة منزلية وهواتف ذكية رائدة عالمياً", "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=300&auto=format&fit=crop&q=80"),
                ("سوني (Sony)", "sony", "صوتيات احترافية ومنصات ألعاب بلايستيشن وكاميرات وتقنيات ترفيهية رائدة", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300&auto=format&fit=crop&q=80"),
                ("نايكي (Nike)", "nike", "العلامة الرياضية الأولى عالمياً للأحذية والملابس الرياضية المبتكرة ذات الأداء العالي", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=300&auto=format&fit=crop&q=80"),
                ("أديداس (Adidas)", "adidas", "تصاميم رياضية أيقونية وأداء استثنائي لجميع الرياضيين وعشاق الأناقة اليومية", "https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=300&auto=format&fit=crop&q=80"),
                ("زارا (Zara)", "zara", "أحدث خطوط الموضة والأزياء الأوروبية الراقية للرجال والنساء", "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=300&auto=format&fit=crop&q=80"),
                ("ديور (Dior)", "dior", "دار الأزياء والعطور الفرنسية الفاخرة ذات اللمسات الأسطورية التي لا تُنسى", "https://images.unsplash.com/photo-1541643600914-78b084683601?w=300&auto=format&fit=crop&q=80"),
                ("ديل (Dell)", "dell", "حواسيب محمولة ومكتبية قوية وشاشات متميزة للمحترفين والمصممين", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80"),
                ("شاومي (Xiaomi)", "xiaomi", "أجهزة ذكية متطورة وهواتف رائدة وأجهزة منزلية عملية بقيمة لا تُضاهى", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=300&auto=format&fit=crop&q=80"),
                ("أسوس (Asus)", "asus", "أجهزة جيمنج ولابتوبات روج زيفيروس الفائقة للمحترفين وهواة الألعاب", "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=300&auto=format&fit=crop&q=80"),
                ("لينوفو (Lenovo)", "lenovo", "أجهزة ThinkPad وLegion القوية للإنتاجية العالية والألعاب", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80"),
                ("إتش بي (HP)", "hp", "أجهزة حواسيب محمولة وطابعات متطورة للمكاتب والأعمال", "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80"),
                ("إل جي (LG)", "lg", "شاشات OLED فائقة النقاء وأجهزة منزلية تكنولوجية متقدمة", "https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=300&auto=format&fit=crop&q=80"),
                ("أنكر (Anker)", "anker", "العلامة الأولى عالمياً في ملحقات الشحن السريع والشواحن اللاسلكية والباور بانك", "https://images.unsplash.com/photo-1609592424364-c289069d2f2d?w=300&auto=format&fit=crop&q=80"),
                ("فيليبس (Philips)", "philips", "حلول ذكية للمنزل والمطبخ وأجهزة العناية الشخصية المبتكرة", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80"),
                ("بوما (Puma)", "puma", "أحذية وملابس رياضية عصرية مستوحاة من ثقافة الشارع والأداء العالي", "https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=300&auto=format&fit=crop&q=80"),
                ("كاسيو (Casio)", "casio", "ساعات جي شوك اليابانية الأسطورية المقاومة للصدمات والمياه", "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=300&auto=format&fit=crop&q=80"),
                ("رايزر (Razer)", "razer", "أجهزة وملحقات الألعاب الاحترافية المصممة للاعبين بواسطة لاعبين", "https://images.unsplash.com/photo-1612287232230-e1a5f69be844?w=300&auto=format&fit=crop&q=80"),
                ("نسبريسو (Nespresso)", "nespresso", "ماكينات وكبسولات القهوة السويسرية الفاخرة للاستمتاع بكوب قهوة استثنائي", "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=300&auto=format&fit=crop&q=80"),
                ("ديلونجي (De'Longhi)", "delonghi", "ماكينات الإسبريسو والقهوة الإيطالية الأصيلة", "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=300&auto=format&fit=crop&q=80"),
                ("لوريال باريس (L'Oréal)", "loreal", "منتجات العناية بالبشرة والشعر والمستحضرات التجميلية الرائدة عالمياً", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=300&auto=format&fit=crop&q=80"),
                ("ريبان (Ray-Ban)", "ray-ban", "النظارات الشمسية والطبية الإيطالية الأيقونية بتصاميم أفياتور ووايفرر الخالدة", "https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=300&auto=format&fit=crop&q=80"),
                ("كالفن كلاين (Calvin Klein)", "calvin-klein", "دار الأزياء والعطور الأمريكية الراقية ذات التصاميم الأنيقة والمعاصرة", "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=300&auto=format&fit=crop&q=80"),
                ("أوبو (Oppo)", "oppo", "هواتف ذكية مبتكرة وتقنيات تصوير بورتريه وشحن سريع فائق", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=300&auto=format&fit=crop&q=80"),
                ("هواوي (Huawei)", "huawei", "ساعات ذكية وأجهزة لوحية متطورة بأنظمة ذكية متكاملة", "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=300&auto=format&fit=crop&q=80"),
                ("جي بي إل (JBL)", "jbl", "مكبرات صوت وسماعات بلوتوث متميزة بصوت Bass قوي ونقي", "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=300&auto=format&fit=crop&q=80"),
                ("بيتس (Beats)", "beats", "سماعات رأس لاسلكية احترافية بصوت استوديو عالي الأداء", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=300&auto=format&fit=crop&q=80"),
                ("أمازون (Amazon)", "amazon", "أجهزة المساعد المنزلي الذكي إيكو وشاشات ذكية متطورة", "https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=300&auto=format&fit=crop&q=80"),
                ("بوش (Bosch)", "bosch", "أجهزة منزلية وأدوات مطبخ ألمانية فائقة الجودة والمتانة", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80"),
                ("آيكيا (IKEA)", "ikea", "أثاث وديكورات وحلول منزلية عملية ومبتكرة تناسب كل بيت", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80"),
                ("نول (Knoll)", "knoll", "أثاث وتصاميم معمارية فاخرة وخالدة لكبار المصممين العالميين", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80"),
                ("أنيبالي كولومبو (Annibale Colombo)", "annibale-colombo", "أثاث إيطالي فاخر مصنوع يدوياً من أندر الأخشاب الطبيعية", "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=300&auto=format&fit=crop&q=80"),
                ("غوتشي (Gucci)", "gucci", "دار الأزياء والعطور الإيطالية الفاخرة ذات التصاميم الجريئة", "https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=300&auto=format&fit=crop&q=80"),
                ("شانيل (Chanel)", "chanel", "أفخم العطور والأزياء الراقية ذات الهيبة والجاذبية الخالدة", "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=300&auto=format&fit=crop&q=80"),
                ("برادا (Prada)", "prada", "دار أزياء وعطور إيطالية رائدة ومبتكرة في عالم الموضة", "https://images.unsplash.com/photo-1541643600914-78b084683601?w=300&auto=format&fit=crop&q=80"),
                ("دولتشي آند غابانا (Dolce & Gabbana)", "dolce-gabbana", "أزياء وعطور إيطالية مفعمة بالحيوية والشغف المتوسطي", "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=300&auto=format&fit=crop&q=80"),
                ("رولكس (Rolex)", "rolex", "الساعات السويسرية الفاخرة رمز الدقة والتميز والريادة", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=300&auto=format&fit=crop&q=80"),
                ("لونجين (Longines)", "longines", "ساعات سويسرية عريقة تجمع بين الأناقة الكلاسيكية والأداء العالي", "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=300&auto=format&fit=crop&q=80"),
                ("آي دبليو سي (IWC)", "iwc", "ساعات شافهاوزن السويسرية الهندسية الفاخرة للطيارين والبحارة", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=300&auto=format&fit=crop&q=80"),
                ("نيو بالانس (New Balance)", "new-balance", "أحذية جري وسنيكرز أيقونية تجمع بين الراحة الفائقة والتصميم الكلاسيكي", "https://images.unsplash.com/photo-1539185441755-769473a23570?w=300&auto=format&fit=crop&q=80"),
                ("أوف وايت (Off-White)", "off-white", "أزياء الشارع الراقية والإكسسوارات العصرية المبتكرة", "https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=300&auto=format&fit=crop&q=80"),
                ("هيشي (Heshe)", "heshe", "حقائب جلدية أصلية فاخرة للسيدات والمسافرين", "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=300&auto=format&fit=crop&q=80"),
                ("فازلين (Vaseline)", "vaseline", "العناية الفائقة بترطيب وحماية البشرة الجافة", "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=300&auto=format&fit=crop&q=80"),
                ("أولاي (Olay)", "olay", "كريمات ومستحضرات مكافحة علامات تقدم السن وتجديد خلايا البشرة", "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=300&auto=format&fit=crop&q=80"),
                ("إيسنس (Essence)", "essence", "مستحضرات مكياج وتجميل عصرية بجودة ممتازة وأسعار مناسبة", "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=300&auto=format&fit=crop&q=80"),
                ("أتيتيود (Attitude)", "attitude", "منتجات طبيعية ونباتية آمنة للعناية بالجسم والمنزل", "https://images.unsplash.com/photo-1621607512214-68297480165e?w=300&auto=format&fit=crop&q=80"),
                ("جيجابايت (Gigabyte)", "gigabyte", "كروت شاشات ولوحات أم متطورة لمحترفي الألعاب", "https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=300&auto=format&fit=crop&q=80"),
                ("ريلمي (realme)", "realme", "هواتف ذكية شبابية بأداء قوي وتصميم مستقبلي رائع", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=300&auto=format&fit=crop&q=80"),
                ("فيفو (vivo)", "vivo", "هواتف ذكية رائدة في تصوير البورتريه الليلي والتصميم النحيف", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=300&auto=format&fit=crop&q=80"),
                ("دودج (Dodge)", "dodge", "سيارات عضلات أمريكية ذات أداء رياضي جبار ومظهر مهيب", "https://cdn.dummyjson.com/product-images/vehicle/charger-sxt-rwd/1.webp"),
                ("كرايسلر (Chrysler)", "chrysler", "سيارات سيدان أمريكية فاخرة بمحركات قوية ومقصورة مريحة", "https://cdn.dummyjson.com/product-images/vehicle/charger-sxt-rwd/1.webp"),
                ("كاواساكي (Kawasaki)", "kawasaki", "دراجات نارية يابانية فائقة السرعة والأداء للسباقات والشوارع", "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=300&auto=format&fit=crop&q=80"),
                ("هوندا (Honda)", "honda", "دراجات نارية وسيارات موثوقة وعالية الكفاءة ومعتمدة عالمياً", "https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=300&auto=format&fit=crop&q=80"),
                ("نسكافيه (Nescafé)", "nescafe", "القهوة سريعة التحضير الأكثر شعبية بنكهاتها الغنية والمتنوعة", "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=300&auto=format&fit=crop&q=80")
            };

            foreach (var b in seedBrands)
            {
                var existing = await db.Brands.FirstOrDefaultAsync(brand => brand.Slug == b.slug);
                if (existing == null)
                {
                    existing = new Brand
                    {
                        Id = Guid.NewGuid(),
                        Name = b.name,
                        Slug = b.slug,
                        Description = b.desc,
                        ImageUrl = b.img,
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    await db.Brands.AddAsync(existing);
                }
                else
                {
                    existing.Name = b.name;
                    existing.Description = b.desc;
                    existing.ImageUrl = b.img;
                    existing.IsActive = true;
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded all {Count} brand entities", seedBrands.Count);
        }

        private async Task SeedProductsAsync(ApplicationDbContext db)
        {
            var mainWarehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "WH-MAIN")
                                ?? await db.Warehouses.FirstOrDefaultAsync();
            if (mainWarehouse == null) return;

            var categories = await db.Categories.ToListAsync();
            var brands = await db.Brands.ToListAsync();

            Guid? GetCatId(string slug) => categories.FirstOrDefault(c => c.Slug == slug)?.Id;
            Guid? GetBrandId(string slug) => brands.FirstOrDefault(b => b.Slug == slug)?.Id;

            var attributeCache = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);

            var productsToSeed = new List<(
                Product product,
                string categorySlug,
                string? brandSlug,
                List<(string url, bool isPrimary, string alt)> images,
                List<(string name, string sku, decimal price, decimal compareAt, (string attribute, string code, string value)[] options)> variants,
                int stock
            )>
            {
                // ==========================================
                // 1. SMARTPHONES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "آيفون 15 برو ماكس (iPhone 15 Pro Max)",
                        Slug = "iphone-15-pro-max",
                        Sku = "APL-IP15PM-256",
                        ShortDescription = "أقوى هاتف من آبل بهيكل التيتانيوم وشريحة A17 Pro الخارقة وكاميرا 5X تقريب بصري.",
                        Description = "يأتي هاتف آيفون 15 برو ماكس بتصميم متطور من التيتانيوم المستخدم في صناعة الطيران والفضاء، مما يجعله أخف وزناً وأقوى متانة. مزود بشريحة A17 Pro الثورية التي تقدم أداءً لا مثيل له للألعاب والمهام الاحترافية، مع نظام كاميرات متطور يدعم دقة 48 ميجابكسل وتقريب بصري حتى 5 أضعاف.",
                        BasePrice = 1199m, CostPrice = 899m, CompareAtPrice = 1299m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true, AllowBackorder = true,
                        AverageRating = 4.9m, ReviewCount = 38, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "apple",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=80", true, "آيفون 15 برو ماكس من الأمام والخلف"),
                        ("https://images.unsplash.com/photo-1695048065059-d2d8ceeb0f2c?w=800&auto=format&fit=crop&q=80", false, "آيفون 15 برو ماكس تيتانيوم طبيعي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("تيتانيوم طبيعي 256 جيجابايت", "APL-IP15PM-256-NAT", 1199m, 1299m, new[] { ("اللون", "COLOR", "تيتانيوم طبيعي"), ("سعة التخزين", "STORAGE", "256 جيجابايت") }),
                        ("تيتانيوم أسود 512 جيجابايت", "APL-IP15PM-512-BLK", 1399m, 1499m, new[] { ("اللون", "COLOR", "تيتانيوم أسود"), ("سعة التخزين", "STORAGE", "512 جيجابايت") })
                    },
                    45
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سامسونج جالكسي S24 ألترا الذكي (Samsung Galaxy S24 Ultra)",
                        Slug = "samsung-galaxy-s24-ultra",
                        Sku = "SMS-S24U-256",
                        ShortDescription = "هاتف سامسونج الرائد بميزات الذكاء الاصطناعي Galaxy AI وهيكل التيتانيوم وقلم S Pen مدمج.",
                        Description = "استكشف آفاقاً جديدة مع هاتف Samsung Galaxy S24 Ultra المزود بميزات الذكاء الاصطناعي الثورية مثل الترجمة الفورية والبحث عبر دائرة على الشاشة ومساعد الصور الذكي. شاشة Dynamic AMOLED 2X مسطحة 6.8 بوصة وكاميرا احترافية بدقة 200 ميجابكسل.",
                        BasePrice = 1299m, CostPrice = 950m, CompareAtPrice = 1399m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true, AllowBackorder = true,
                        AverageRating = 4.9m, ReviewCount = 35, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "samsung",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=800&auto=format&fit=crop&q=80", true, "سامسونج جالكسي S24 ألترا"),
                        ("https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800&auto=format&fit=crop&q=80", false, "تفاصيل كاميرا سامسونج")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي تيتانيوم 256 جيجابايت", "SMS-S24U-256-GRY", 1299m, 1399m, new[] { ("اللون", "COLOR", "رمادي تيتانيوم"), ("سعة التخزين", "STORAGE", "256 جيجابايت") }),
                        ("أسود تيتانيوم 512 جيجابايت", "SMS-S24U-512-BLK", 1450m, 1550m, new[] { ("اللون", "COLOR", "أسود تيتانيوم"), ("سعة التخزين", "STORAGE", "512 جيجابايت") })
                    },
                    50
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "شاومي ريدمي نوت 13 برو بلس 5G (Xiaomi Redmi Note 13 Pro+)",
                        Slug = "xiaomi-redmi-note-13-pro-plus",
                        Sku = "XMI-RN13P-5G",
                        ShortDescription = "هاتف شاومي المتطور بكاميرا 200MP مع مثبت بصري وشحن فائق السرعة بقدرة 120W HyperCharge.",
                        Description = "يقدم Redmi Note 13 Pro+ تجربة رائدة بفضل شاشة AMOLED منحنية 1.5K بمعدل 120Hz ومعالج Dimensity 7200 Ultra ومقاومة للماء والغبار IP68.",
                        BasePrice = 399m, CostPrice = 270m, CompareAtPrice = 449m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 28, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "xiaomi",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&auto=format&fit=crop&q=80", true, "شاومي ريدمي نوت 13 برو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود منتصف الليل 256GB", "XMI-RN13P-BLK", 399m, 449m, new[] { ("اللون", "COLOR", "أسود منتصف الليل") }),
                        ("بنفسجي أورورا 512GB", "XMI-RN13P-PUR", 459m, 499m, new[] { ("اللون", "COLOR", "بنفسجي أورورا") })
                    },
                    40
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "أوبو رينو 11 برو 5G (Oppo Reno 11 Pro 5G)",
                        Slug = "oppo-reno-11-pro-5g",
                        Sku = "OPP-RENO11P-5G",
                        ShortDescription = "خبير تصوير البورتريه بتصميم نحيف وشاشة AMOLED منحنية 120Hz وشحن 80W SUPERVOOC.",
                        Description = "يتميز هاتف Oppo Reno 11 Pro بكاميرا بورتريه تليفوتوغرافي بدقة 32MP مع مستشعر Sony IMX709 وتقنيات تعديل ذكية للصور ومحرك أداء فائق وسريع.",
                        BasePrice = 479m, CostPrice = 320m, CompareAtPrice = 529m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 15, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "oppo",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=800&auto=format&fit=crop&q=80", true, "هاتف أوبو رينو 11 برو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    30
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ريلمي 12 برو بلس 5G (realme 12 Pro+ 5G)",
                        Slug = "realme-12-pro-plus-5g",
                        Sku = "RLM-12PP-5G",
                        ShortDescription = "هاتف بتصميم الساعات السويسرية الفاخرة وكاميرا Periscope تليفوتوغرافي مقربة.",
                        Description = "تصميم فريد من الجلد النباتي الفاخر بالتعاون مع مصممي الساعات السويسرية، مع كاميرا بيريسكوب متطورة للتقريب البصري حتى 120X.",
                        BasePrice = 389m, CostPrice = 260m, CompareAtPrice = 429m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.6m, ReviewCount = 12, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "realme",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1580910051074-3eb694886505?w=800&auto=format&fit=crop&q=80", true, "هاتف ريلمي 12 برو بلس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    25
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "فيفو V30 الذكي 5G (vivo V30 5G)",
                        Slug = "vivo-v30-5g",
                        Sku = "VIV-V30-5G",
                        ShortDescription = "إضاءة Aura Light البورتريه الذكية وتصميم فائق النحافة وبطارية 5000mAh.",
                        Description = "يمنحك vivo V30 إضاءة استوديو متكاملة في جيبك بفضل حلقة Aura Light المدمجة مع كاميرا سيلفي بدقة 50MP بزاوية عريضة وتصميم زجاجي عاكس رائع.",
                        BasePrice = 419m, CostPrice = 280m, CompareAtPrice = 469m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 14, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smartphones", "vivo",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1565849904461-04a58ad377e0?w=800&auto=format&fit=crop&q=80", true, "هاتف فيفو V30")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    25
                ),

                // ==========================================
                // 2. PHONE ACCESSORIES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "شاحن باور بانك مغناطيسي لاسلكي أنكر ماج جو 10000 مللي أمبير (Anker MagGo)",
                        Slug = "anker-maggo-power-bank-10000mah",
                        Sku = "ANK-MAGGO-10K",
                        ShortDescription = "شاحن لاسلكي مغناطيسي سريع بقوة 15 واط معتمد من Qi2 وشاشة رقمية ذكية توضح نسبة الشحن.",
                        Description = "اشحن هاتف الآيفون أو الأجهزة المتوافقة بسرعة فائقة مع شاحن Anker MagGo اللاسلكي المغناطيسي. بفضل تقنية Qi2 والشاشة الذكية لعرض الطاقة المتبقية بدقة.",
                        BasePrice = 79m, CostPrice = 42m, CompareAtPrice = 99m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 41, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "phone-accessories", "anker",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1609592424364-c289069d2f2d?w=800&auto=format&fit=crop&q=80", true, "شاحن باور بانك أنكر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود كلاسيكي", "ANK-MAGGO-BLK", 79m, 99m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أبيض لؤلؤي", "ANK-MAGGO-WHT", 79m, 99m, new[] { ("اللون", "COLOR", "أبيض لؤلؤي") })
                    },
                    120
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "شاحن أبل ماج سيف اللاسلكي 15 واط (Apple MagSafe Charger)",
                        Slug = "apple-magsafe-wireless-charger",
                        Sku = "APL-MAGSAFE-15W",
                        ShortDescription = "شاحن لاسلكي مغناطيسي أصلي من أبل يلتصق بدقة بهواتف آيفون للشحن السريع والآمن.",
                        Description = "يوفر شاحن MagSafe شحناً لاسلكياً سريعاً ومحاذاة مغناطيسية مثالية لجميع أجهزة iPhone 12 فما فوق وعلب سماعات AirPods المتوافقة.",
                        BasePrice = 39m, CostPrice = 22m, CompareAtPrice = 49m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 32, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "phone-accessories", "apple",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1583863788434-e58a36330cf0?w=800&auto=format&fit=crop&q=80", true, "شاحن ماج سيف أبل")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    80
                ),

                // ==========================================
                // 3. LAPTOPS & COMPUTERS
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "لابتوب ديل XPS 15 إنش شاشة 3.5K OLED (Dell XPS 15)",
                        Slug = "dell-xps-15-oled-laptop",
                        Sku = "DLL-XPS15-OLED",
                        ShortDescription = "حاسوب محمول فائق الأداء بمعالج Intel Core i7 وبطاقة شاشة RTX 4060 وشاشة OLED مذهلة.",
                        Description = "يعد Dell XPS 15 الخيار المثالي للمصممين والمبرمجين وصناع المحتوى، حيث يجمع بين هيكل أنيق من الألمنيوم وألياف الكربون، وشاشة OLED مذهلة بدقة 3.5K تدعم نطاق ألوان DCI-P3 بنسبة 100%.",
                        BasePrice = 1499m, CostPrice = 1100m, CompareAtPrice = 1699m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 12, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "laptops", "dell",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80", true, "لابتوب ديل XPS 15")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("معالج i7 - رام 16GB - سعة 512GB SSD", "DLL-XPS15-16-512", 1499m, 1699m, new[] { ("المواصفات", "SPEC", "i7 / 16GB / 512GB SSD") }),
                        ("معالج i9 - رام 32GB - سعة 1TB SSD", "DLL-XPS15-32-1TB", 1899m, 2099m, new[] { ("المواصفات", "SPEC", "i9 / 32GB / 1TB SSD") })
                    },
                    25
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ماك بوك برو 16 إنش شريحة M3 Pro الخارقة (MacBook Pro 16 M3 Pro)",
                        Slug = "macbook-pro-16-m3-pro",
                        Sku = "APL-MBP16-M3P",
                        ShortDescription = "لابتوب أبل الاحترافي للمبدعين والمبرمجين مع شاشة Liquid Retina XDR وبطارية تدوم 22 ساعة.",
                        Description = "يقدم جهاز MacBook Pro مقاس 16 بوصة أداءً استثنائياً بفضل شريحة M3 Pro المتطورة، مع شاشة Liquid Retina XDR فائقة السطوع ونظام صوتي مكون من 6 مكبرات صوت مع دعم الصوت المكاني.",
                        BasePrice = 2499m, CostPrice = 1900m, CompareAtPrice = 2699m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 27, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "laptops", "apple",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80", true, "ماك بوك برو 16 إنش")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود فلكي - 18GB رام - 512GB SSD", "APL-MBP16-18-512-BLK", 2499m, 2699m, new[] { ("اللون", "COLOR", "أسود فلكي"), ("المواصفات", "SPEC", "18GB RAM / 512GB SSD") }),
                        ("فضي - 36GB رام - 1TB SSD", "APL-MBP16-36-1TB-SLV", 2899m, 3099m, new[] { ("اللون", "COLOR", "فضي"), ("المواصفات", "SPEC", "36GB RAM / 1TB SSD") })
                    },
                    20
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "لابتوب الألعاب أسوس روج زيفيروس G16 (Asus ROG Zephyrus G16)",
                        Slug = "asus-rog-zephyrus-g16",
                        Sku = "ASUS-ROG-G16-RTX",
                        ShortDescription = "لابتوب ألعاب خارق بشاشة OLED 240Hz ومعالج Intel Core Ultra 9 وبطاقة RTX 4070.",
                        Description = "صُمم لابتوب Asus ROG Zephyrus G16 ليقدم أقصى درجات القوة في هيكل فائق النحافة من الألمنيوم المصقول، مع شاشة ROG Nebula OLED بدقة 2.5K ومعدل تحديث 240Hz.",
                        BasePrice = 1999m, CostPrice = 1550m, CompareAtPrice = 2299m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 16, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "laptops", "asus",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=800&auto=format&fit=crop&q=80", true, "لابتوب ألعاب أسوس روج")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي إكليبس - RTX 4070", "ASUS-G16-4070-GRY", 1999m, 2299m, new[] { ("اللون", "COLOR", "رمادي إكليبس") }),
                        ("أبيض بلاتيني - RTX 4080", "ASUS-G16-4080-WHT", 2499m, 2799m, new[] { ("اللون", "COLOR", "أبيض بلاتيني") })
                    },
                    18
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "لينوفو ثينك باد X1 كربون الجيل 11 (Lenovo ThinkPad X1 Carbon)",
                        Slug = "lenovo-thinkpad-x1-carbon-gen11",
                        Sku = "LNV-X1C-G11",
                        ShortDescription = "حاسوب الأعمال الأسطوري خفيف الوزن بوزن 1.12 كجم فقط وهيكل من ألياف الكربون المقوى.",
                        Description = "يقدم ThinkPad X1 Carbon Gen 11 أعلى مستويات الأمان والإنتاجية بفضل معالجات Intel Core vPro وشاشة 2.8K OLED ولوحة مفاتيح ThinkPad الشهيرة بمقاومة السوائل.",
                        BasePrice = 1649m, CostPrice = 1200m, CompareAtPrice = 1849m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 21, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "laptops", "lenovo",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80", true, "لابتوب لينوفو ثينك باد")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    22
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "لابتوب إتش بي سبيكتر x360 المتحول 2 في 1 (HP Spectre x360 14)",
                        Slug = "hp-spectre-x360-14-oled",
                        Sku = "HP-SPEC-X360",
                        ShortDescription = "لابتوب شاشة لمس متحولة 360 درجة بدقة 2.8K OLED مع قلم ذكي وكاميرا 9MP بتقنيات الذكاء الاصطناعي.",
                        Description = "تحفة هندسية من الألمنيوم المصقول بتقنية CNC مع شاشة لمس OLED مذهلة ومفصلة مرنة تتيح استخدامه كلابتوب أو تابلت بكل سلاسة.",
                        BasePrice = 1449m, CostPrice = 1050m, CompareAtPrice = 1629m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 19, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "laptops", "hp",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&auto=format&fit=crop&q=80", true, "لابتوب إتش بي سبيكتر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    20
                ),

                // ==========================================
                // 4. TABLETS
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "آيباد ميني الجيل السادس (Apple iPad Mini 6)",
                        Slug = "apple-ipad-mini-6",
                        Sku = "APL-IPADMINI6",
                        ShortDescription = "جهاز آيباد ميني بشاشة Liquid Retina 8.3 إنش وشريحة A15 Bionic ودعم قلم Apple Pencil 2.",
                        Description = "يضع جهاز iPad Mini كل قوة آيباد في راحة يد واحدة. تصميم رائع بحواف نحيفة وشاشة ممتدة بالحواف ومنفذ USB-C ونظام كاميرات عريضة جداً مع خاصية Center Stage.",
                        BasePrice = 499m, CostPrice = 380m, CompareAtPrice = 549m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 30, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "tablets", "apple",
                    new List<(string, bool, string)> {
                        ("https://cdn.dummyjson.com/product-images/tablets/ipad-mini-2021-starlight/1.webp", true, "آيبad ميني ستارلايت من الأمام"),
                        ("https://cdn.dummyjson.com/product-images/tablets/ipad-mini-2021-starlight/2.webp", false, "آيباد ميني من الخلف")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("سعة 64GB - ستارلايت", "APL-IPADMINI6-64", 499m, 549m, new[] { ("سعة التخزين", "STORAGE", "64GB") }),
                        ("سعة 256GB - رمادي فلكي", "APL-IPADMINI6-256", 649m, 699m, new[] { ("سعة التخزين", "STORAGE", "256GB") })
                    },
                    30
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سامسونج جالكسي تاب S9 ألترا (Samsung Galaxy Tab S9 Ultra)",
                        Slug = "samsung-galaxy-tab-s9-ultra",
                        Sku = "SMS-TABS9U",
                        ShortDescription = "تابلت بشاشة عملاقة 14.6 بوصة Dynamic AMOLED 2X ومقاومة للماء IP68 وقلم S Pen مرفق.",
                        Description = "استمتع بمساحة عمل وإبداع غير مسبوقة مع أكبر شاشة تابلت في العالم. معالج Snapdragon 8 Gen 2 وصوت رباعي من AKG مع دعم وضع Samsung DeX لتجربة مكتبية متكاملة.",
                        BasePrice = 1099m, CostPrice = 820m, CompareAtPrice = 1199m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 22, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "tablets", "samsung",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80", true, "تابلت سامسونج جالكسي تاب S9 ألترا")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    25
                ),

                // ==========================================
                // 5. AUDIO & HEADPHONES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سماعات سوني اللاسلكية WH-1000XM5 عازلة للضوضاء",
                        Slug = "sony-wh-1000xm5-headphones",
                        Sku = "SNY-WH1000XM5",
                        ShortDescription = "سماعات رأس لاسلكية متميزة بإلغاء الضوضاء الرائد في الصناعة وصوت عالي الدقة وبطارية تدوم 30 ساعة.",
                        Description = "تعيد سماعات الرأس اللاسلكية WH-1000XM5 من سوني كتابة قواعد الاستماع بدون تشتيت، بفضل معالجين متطورين و8 ميكروفونات لعزل الضوضاء تلقائياً. توفر راحة استثنائية عند ارتدائها طوال اليوم مع جودة مكالمات فائقة الوضوح.",
                        BasePrice = 349m, CostPrice = 220m, CompareAtPrice = 399m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 24, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "audio-headphones", "sony",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80", true, "سماعات سوني WH-1000XM5")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود كلاسيكي (Black)", "SNY-WH1000XM5-BLK", 349m, 399m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("فضي بلاتيني (Silver)", "SNY-WH1000XM5-SLV", 349m, 399m, new[] { ("اللون", "COLOR", "فضي بلاتيني") })
                    },
                    60
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سماعات أبل إيربودز برو الجيل الثاني USB-C (AirPods Pro 2)",
                        Slug = "apple-airpods-pro-2-usbc",
                        Sku = "APL-AIRPODS-PRO2",
                        ShortDescription = "سماعات أبل اللاسلكية الرائدة بإلغاء ضوضاء نشط مضاعف وصوت تكيفي ومنفذ USB-C.",
                        Description = "تقدم سماعات AirPods Pro الجيل الثاني مستوى استثنائياً من عزل الضوضاء النشط والصوت التكيفي الذي يمزج بين شفافية الصوت وإلغاء الضوضاء بذكاء حسب البيئة المحيطة.",
                        BasePrice = 249m, CostPrice = 175m, CompareAtPrice = 279m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 48, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "audio-headphones", "apple",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800&auto=format&fit=crop&q=80", true, "سماعات أبل إيربودز برو 2")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    85
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سماعات بيتس ستوديو برو اللاسلكية (Beats Studio Pro)",
                        Slug = "beats-studio-pro-wireless",
                        Sku = "BTS-STUDIO-PRO",
                        ShortDescription = "صوت عالي الدقة Lossless عبر USB-C مع إلغاء الضوضاء النشط وصوت مكاني مخصص 360 درجة.",
                        Description = "توفر سماعات Beats Studio Pro تجربة صوتية متطورة مع مشغلات مخصصة 40 مم وتوافق سلس بلمسة واحدة مع أجهزة أبل وأندرويد وبطارية تدوم حتى 40 ساعة.",
                        BasePrice = 299m, CostPrice = 195m, CompareAtPrice = 349m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 18, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "audio-headphones", "beats",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=800&auto=format&fit=crop&q=80", true, "سماعات بيتس ستوديو برو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود غير لامع", "BTS-STDP-BLK", 299m, 349m, new[] { ("اللون", "COLOR", "أسود غير لامع") }),
                        ("بني كراميل داكن", "BTS-STDP-BRN", 299m, 349m, new[] { ("اللون", "COLOR", "بني كراميل") })
                    },
                    40
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "مكبر صوت بلوتوث محمول جي بي إل فليب 6 (JBL Flip 6)",
                        Slug = "jbl-flip-6-portable-speaker",
                        Sku = "JBL-FLIP6-BT",
                        ShortDescription = "مكبر صوت مقاوم للماء والغبار IP67 بصوت قوي وعميق وبطارية تدوم 12 ساعة متواصلة.",
                        Description = "يتميز JBL Flip 6 بنظام مكبرات صوت ثنائي الاتجاه يوفر صوتاً عالي الوضوح ونقياً مع صوت جهير Bass استثنائي وقدرة ربط PartyBoost لدمج مكبرات متعددة.",
                        BasePrice = 119m, CostPrice = 65m, CompareAtPrice = 139m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 27, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "audio-headphones", "jbl",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1545454675-3531b543be5d?w=800&auto=format&fit=crop&q=80", true, "مكبر صوت جي بي إل فليب 6")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أزرق مائي", "JBL-FLIP6-BLU", 119m, 139m, new[] { ("اللون", "COLOR", "أزرق مائي") }),
                        ("أسود مميز", "JBL-FLIP6-BLK", 119m, 139m, new[] { ("اللون", "COLOR", "أسود مميز") })
                    },
                    55
                ),

                // ==========================================
                // 6. TV & DISPLAYS
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "تلفزيون سامسونج الذكي 55 بوصة OLED 4K (Samsung Smart TV)",
                        Slug = "samsung-55-inch-oled-4k-tv",
                        Sku = "SMS-TV-55OLED",
                        ShortDescription = "تلفزيون سامسونج أوليد بدقة 4K فائقة الوضوح مع معالج Neural Quantum ومعدل تحديث 120Hz.",
                        Description = "استمتع بتجربة سينمائية لا مثيل لها في منزلك مع تلفزيون سامسونج OLED مقاس 55 بوصة. درجات سواد لا نهائية وألوان مفعمة بالحياة بفضل تقنية النقاط الكمية Quantum Dot.",
                        BasePrice = 1199m, CostPrice = 850m, CompareAtPrice = 1499m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 18, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "tv-displays", "samsung",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=800&auto=format&fit=crop&q=80", true, "شاشة تلفزيون سامسونج ذكية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    20
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "تلفزيون إل جي الذكي 65 بوصة OLED evo C3 بدقة 4K (LG OLED evo C3)",
                        Slug = "lg-oled-evo-c3-65-inch-tv",
                        Sku = "LG-OLED-65C3",
                        ShortDescription = "شاشة OLED evo الأكثر مبيعاً بمعالج α9 AI 4K Gen6 ودعم كامل لتقنيات Dolby Vision وDolby Atmos.",
                        Description = "يقدم تلفزيون LG C3 OLED صوراً مذهلة مع تعزيز السطوع Brightness Booster ومعدل تحديث 120Hz و4 منافذ HDMI 2.1 كاملة لتجربة ألعاب لا تضاهى على PS5 وXbox.",
                        BasePrice = 1599m, CostPrice = 1150m, CompareAtPrice = 1899m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 24, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "tv-displays", "lg",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1461151304267-38535e780c79?w=800&auto=format&fit=crop&q=80", true, "تلفزيون إل جي أوليد")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    15
                ),

                // ==========================================
                // 7. GAMING & CONSOLES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "جهاز ألعاب بلايستيشن 5 سليم سعة 1 تيرابايت (Sony PlayStation 5 Slim)",
                        Slug = "sony-playstation-5-slim",
                        Sku = "SNY-PS5-SLIM-1TB",
                        ShortDescription = "منصة الألعاب الأكثر شعبية في العالم بتصميم نحيف وسعة تخزين 1TB ودعم رسومات 4K 120Hz.",
                        Description = "عش تجربة ألعاب غامرة لم يسبق لها مثيل مع جهاز PS5 Slim. استمتع بأوقات تحميل شبه فورية مع وحدة تخزين SSD فائقة السرعة، وردود فعل لمسية غامرة ومحفزات تكيفية مع ذراع التحكم اللاسلكي DualSense.",
                        BasePrice = 499m, CostPrice = 420m, CompareAtPrice = 549m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 52, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "gaming-consoles", "sony",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=800&auto=format&fit=crop&q=80", true, "جهاز بلايستيشن 5 سليم")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("النسخة الرقمية (Digital Edition)", "SNY-PS5-SLIM-DIG", 449m, 499m, new[] { ("الإصدار", "EDITION", "النسخة الرقمية") }),
                        ("نسخة محرك الأقراص (Disc Edition)", "SNY-PS5-SLIM-DISC", 499m, 549m, new[] { ("الإصدار", "EDITION", "نسخة محرك الأقراص") })
                    },
                    35
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سماعات الألعاب اللاسلكية رايزر بلاك شارك V2 برو (Razer BlackShark V2 Pro)",
                        Slug = "razer-blackshark-v2-pro",
                        Sku = "RZR-BSV2P-WL",
                        ShortDescription = "سماعة ألعاب تنافسية احترافية بمحركات TriForce Titanium 50mm وميكروفون فائق النقاء.",
                        Description = "إذا كانت الرياضات الإلكترونية هي شغفك، فإن Razer BlackShark V2 Pro هي سلاحك المفضل. عزل صوتي متفوق وراحة لا تضاهى لوسائد الأذن وبطارية تدوم حتى 70 ساعة.",
                        BasePrice = 199m, CostPrice = 125m, CompareAtPrice = 229m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 20, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "gaming-consoles", "razer",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1612287232230-e1a5f69be844?w=800&auto=format&fit=crop&q=80", true, "سماعة جيمنج رايزر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود كلاسيكي", "RZR-BSV2P-BLK", 199m, 229m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أبيض ميركوري", "RZR-BSV2P-WHT", 199m, 229m, new[] { ("اللون", "COLOR", "أبيض ميركوري") })
                    },
                    45
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "كرت شاشة جيجابايت أوروس RTX 4080 سوبر 16GB (Gigabyte AORUS RTX 4080 Super)",
                        Slug = "gigabyte-aorus-rtx-4080-super",
                        Sku = "GIG-AORUS-4080S",
                        ShortDescription = "أقوى بطاقة رسوميات للألعاب بدقة 4K وتقنيات تتبع الأشعة DLSS 3.5 وتبريد WINDFORCE المائي.",
                        Description = "تقدم بطاقة Gigabyte AORUS Master RTX 4080 Super أعلى معدلات الإطارات مع شاشة LCD Edge View لعرض درجات الحرارة وتصميم RGB Fusion مبهر.",
                        BasePrice = 1149m, CostPrice = 920m, CompareAtPrice = 1299m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 14, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "gaming-consoles", "gigabyte",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=800&auto=format&fit=crop&q=80", true, "كرت شاشة جيجابايت أوروس RTX 4080")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    12
                ),

                // ==========================================
                // 8. SMART HOME
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "مكنسة روبوتية ذكية شاومي ممسحة ومكنسة S10 (Xiaomi Robot Vacuum S10)",
                        Slug = "xiaomi-robot-vacuum-s10",
                        Sku = "XMI-ROBOT-S10",
                        ShortDescription = "مكنسة ذكية بنظام الملاحة الليزرية LDS وقوة شفط 4000 باسكال ومسح ذكي متعرج.",
                        Description = "تحكم بنظافة منزلك عن بُعد مع مكنسة Xiaomi Robot Vacuum S10. مزودة برادار ليزري 360 درجة لرسم خرائط دقيقة لمنزلك مع خزان مياه ذكي وقوة شفط فائقة.",
                        BasePrice = 249m, CostPrice = 160m, CompareAtPrice = 299m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 23, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smart-home", "xiaomi",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1518640467707-6811f4a6ab73?w=800&auto=format&fit=crop&q=80", true, "مكنسة روبوتية شاومي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    28
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "مساعد أمازون الذكي إيكو دوت الجيل الخامس مع أليكسا (Amazon Echo Dot 5th Gen)",
                        Slug = "amazon-echo-dot-5th-gen",
                        Sku = "AMZ-ECHODOT-5",
                        ShortDescription = "مكبر صوت ذكي مع أليكسا بصوت أغنى وأعمق ومستشعر درجة حرارة وتحكم كامل بالمنزل الذكي.",
                        Description = "تحكم بإضاءة منزلك ومكيفات الهواء وشغل الموسيقى واسأل عن الأخبار وحالة الطقس بأوامر صوتية سهلة مع مساعد أمازون إيكو دوت.",
                        BasePrice = 49m, CostPrice = 28m, CompareAtPrice = 59m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 45, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "smart-home", "amazon",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1543512214-318c7553f230?w=800&auto=format&fit=crop&q=80", true, "مساعد أمازون الذكي إيكو دوت")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي فحمي", "AMZ-ED5-GRY", 49m, 59m, new[] { ("اللون", "COLOR", "رمادي فحمي") }),
                        ("أزرق مائي", "AMZ-ED5-BLU", 49m, 59m, new[] { ("اللون", "COLOR", "أزرق مائي") })
                    },
                    60
                ),

                // ==========================================
                // 9. MEN'S CLOTHING
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "قميص كلاسيكي قطن أكسفورد من زارا (Zara Oxford Cotton Shirt)",
                        Slug = "zara-oxford-cotton-shirt",
                        Sku = "ZRA-SHIRT-OXF",
                        ShortDescription = "قميص رجالي أنيق بقصة سليم فيت مصنوع من القطن الطبيعي 100% مناسب للعمل والمناسبات.",
                        Description = "يجمع قميص زارا أكسفورد بين الطراز الكلاسيكي المتقن والراحة الفائقة. منسوج من خيوط القطن الطبيعي عالية الجودة مع ياقة بأزرار وأكمام طويلة قابلة للطي.",
                        BasePrice = 45m, CostPrice = 22m, CompareAtPrice = 59m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.6m, ReviewCount = 18, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-clothing", "zara",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1596755094514-f87e34085b2c?w=800&auto=format&fit=crop&q=80", true, "قميص أكسفورد أبيض أنيق")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أبيض - مقاس M", "ZRA-SHIRT-WHT-M", 45m, 59m, new[] { ("اللون", "COLOR", "أبيض"), ("المقاس", "SIZE", "M") }),
                        ("أبيض - مقاس L", "ZRA-SHIRT-WHT-L", 45m, 59m, new[] { ("اللون", "COLOR", "أبيض"), ("المقاس", "SIZE", "L") }),
                        ("أزرق سماوي - مقاس M", "ZRA-SHIRT-BLU-M", 45m, 59m, new[] { ("اللون", "COLOR", "أزرق سماوي"), ("المقاس", "SIZE", "M") })
                    },
                    120
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "هودي رياضي نايكي كلوب فليس بغطاء رأس (Nike Club Fleece Hoodie)",
                        Slug = "nike-club-fleece-hoodie",
                        Sku = "NKE-HOODIE-CF",
                        ShortDescription = "هودي دافئ ومريح بصوف ناعم مصقول وقصة كلاسيكية مميزة مع شعار نايكي المطرز.",
                        Description = "يعد هودي Nike Sportswear Club Fleece قطعة أساسية في خزانة ملابسك، حيث يجمع بين الأناقة اليومية والراحة الفائقة بفضل نسيج الصوف الناعم المصقول من الداخل.",
                        BasePrice = 65m, CostPrice = 32m, CompareAtPrice = 80m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 29, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-clothing", "nike",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80", true, "هودي نايكي رياضي أسود")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود - مقاس M", "NKE-HOOD-BLK-M", 65m, 80m, new[] { ("اللون", "COLOR", "أسود"), ("المقاس", "SIZE", "M") }),
                        ("رمادي - مقاس L", "NKE-HOOD-GRY-L", 65m, 80m, new[] { ("اللون", "COLOR", "رمادي"), ("المقاس", "SIZE", "L") })
                    },
                    95
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حزام أوف وايت إندستريال الأصفر الأيقوني (Off-White Industrial Belt)",
                        Slug = "off-white-industrial-yellow-belt",
                        Sku = "OFF-BELT-IND-YEL",
                        ShortDescription = "الحزام الأكثر شهرة في عالم أزياء الشارع بطول 200 سم ونقوش شعار Off-White المميزة.",
                        Description = "يتميز حزام Off-White Classic Industrial بإبزيم معدني قوي ونمط نسيجي أصفر لافت يضفي لمسة عصرية فريدة على إطلالاتك الكاجوال والرياضية.",
                        BasePrice = 195m, CostPrice = 110m, CompareAtPrice = 240m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 16, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-clothing", "off-white",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1624222247344-550fb60583dc?w=800&auto=format&fit=crop&q=80", true, "حزام أوف وايت أصفر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    30
                ),

                // ==========================================
                // 10. WOMEN'S CLOTHING
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "فستان كتان صيفي أنيق من زارا (Zara Linen Summer Dress)",
                        Slug = "zara-linen-summer-dress",
                        Sku = "ZRA-DRSS-LINEN",
                        ShortDescription = "فستان نسائي كاجوال وناعم من الكتان الطبيعي 100% بقصة مريحة مناسبة للأيام المشمسة.",
                        Description = "تألقي ببساطة مع فستان زارا الكتان الخفيف ذي الياقة المربعة والأحزمة القابلة للتعديل والجيوب الجانبية الخفية.",
                        BasePrice = 69m, CostPrice = 34m, CompareAtPrice = 89m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 23, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-clothing", "zara",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=800&auto=format&fit=crop&q=80", true, "فستان كتان صيفي زارا")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("بيج رملي - S", "ZRA-DRSS-S", 69m, 89m, new[] { ("المقاس", "SIZE", "S") }),
                        ("بيج رملي - M", "ZRA-DRSS-M", 69m, 89m, new[] { ("المقاس", "SIZE", "M") })
                    },
                    50
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حمالة صدر رياضية مريحة كالفن كلاين قطن مودرن (Calvin Klein Modern Cotton Bralette)",
                        Slug = "calvin-klein-modern-cotton-bralette",
                        Sku = "CK-BRALETTE-MOD",
                        ShortDescription = "حمالة صدر نسائية كلاسيكية فائقة النعومة من القطن والمودال مع شريط الخصر الأيقوني.",
                        Description = "تجسيد الراحة اليومية القصوى من كالفن كلاين بقماش مطاطي مسامي داعم وتصميم Racerback رياضي.",
                        BasePrice = 38m, CostPrice = 18m, CompareAtPrice = 48m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 37, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-clothing", "calvin-klein",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1516762689617-e1cffcef479d?w=800&auto=format&fit=crop&q=80", true, "ملابس داخلية كالفن كلاين")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي هيذر - S", "CK-BRA-GRY-S", 38m, 48m, new[] { ("المقاس", "SIZE", "S") }),
                        ("أسود كلاسيكي - M", "CK-BRA-BLK-M", 38m, 48m, new[] { ("المقاس", "SIZE", "M") })
                    },
                    70
                ),

                // ==========================================
                // 11. MEN'S SHOES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حذاء نايكي إير ماكس بلس الرياضي (Nike Air Max Plus)",
                        Slug = "nike-air-max-plus-sneakers",
                        Sku = "NKE-AIRMAX-PLUS",
                        ShortDescription = "حذاء رياضي أنيق ومريح بتقنية Tuned Air لتوفير ثبات وتوسيد فائق أثناء الركض والمشي.",
                        Description = "يتميز حذاء نايكي إير ماكس بلس بتصميم أيقوني مستوحى من أشجار النخيل وأمواج المحيط، مع تقنية Tuned Air التي تقدم توسيداً خفيفاً واستقراراً مذهلاً.",
                        BasePrice = 175m, CostPrice = 95m, CompareAtPrice = 199m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 19, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-shoes", "nike",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&auto=format&fit=crop&q=80", true, "حذاء نايكي إير ماكس أحمر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("مقاس 42", "NKE-AIRMAX-42", 175m, 199m, new[] { ("المقاس", "SIZE", "42") }),
                        ("مقاس 43", "NKE-AIRMAX-43", 175m, 199m, new[] { ("المقاس", "SIZE", "43") })
                    },
                    80
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حذاء أديداس ألترا بوست 1.0 للجري (Adidas Ultraboost 1.0)",
                        Slug = "adidas-ultraboost-1-sneakers",
                        Sku = "ADS-UB-10",
                        ShortDescription = "حذاء الجري الأكثر راحة في العالم بنعل Boost الثوري وجزء علوي محبوك من Primeknit.",
                        Description = "سواء كنت تمارس الجري في الصباح أو تتنقل في مشاويرك اليومية، يمنحك حذاء Adidas Ultraboost 1.0 طاقة متجددة في كل خطوة بفضل مئات كبسولات Boost.",
                        BasePrice = 180m, CostPrice = 95m, CompareAtPrice = 210m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 22, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-shoes", "adidas",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=800&auto=format&fit=crop&q=80", true, "حذاء أديداس ألترا بوست أبيض")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أبيض ناصع - 42", "ADS-UB-WHT-42", 180m, 210m, new[] { ("المقاس", "SIZE", "42") }),
                        ("أسود كور - 43", "ADS-UB-BLK-43", 180m, 210m, new[] { ("المقاس", "SIZE", "43") })
                    },
                    65
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حذاء نيو بالانس 574 الكلاسيكي الأصلي (New Balance 574 Core)",
                        Slug = "new-balance-574-core-sneakers",
                        Sku = "NB-574-CORE",
                        ShortDescription = "السنيكرز الكلاسيكي المصنوع من الشامواه الطبيعي والشبك مع تقنية التوسيد المريحة ENCAP.",
                        Description = "حذاء New Balance 574 هو أيقونة الأحذية غير الرسمية، يجمع بين المظهر التراثي الأنيق والراحة القصوى لنعل أوسط ناعم ومتين.",
                        BasePrice = 89m, CostPrice = 48m, CompareAtPrice = 105m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 31, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-shoes", "new-balance",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1539185441755-769473a23570?w=800&auto=format&fit=crop&q=80", true, "حذاء نيو بالانس 574 رمادي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي كلاسيكي - مقاس 42", "NB-574-GRY-42", 89m, 105m, new[] { ("المقاس", "SIZE", "42") }),
                        ("كحلي - مقاس 43", "NB-574-NVY-43", 89m, 105m, new[] { ("المقاس", "SIZE", "43") })
                    },
                    60
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حذاء بوما آر إس-إكس إفكت الرياضي (Puma RS-X Efekt)",
                        Slug = "puma-rs-x-efekt-sneakers",
                        Sku = "PMA-RSX-EFEKT",
                        ShortDescription = "سنيكرز عصري جريء بنظام التوسيد الشهير Running System وألوان متعددة لافتة للأنظار.",
                        Description = "يعيد حذاء Puma RS-X تعريف أسلوب الشارع بتصميم مستقبلي وطبقات متعددة ونعل سميك ومريح يوفر توسيداً مثالياً للقدم.",
                        BasePrice = 115m, CostPrice = 60m, CompareAtPrice = 135m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 21, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-shoes", "puma",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=800&auto=format&fit=crop&q=80", true, "حذاء بوما آر إس-إكس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    70
                ),

                // ==========================================
                // 12. WOMEN'S SHOES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حذاء نايكي إير فورس 1 '07 أبيض ناصع نسائي (Nike Air Force 1 '07)",
                        Slug = "nike-air-force-1-women-white",
                        Sku = "NKE-AF1-WHT-W",
                        ShortDescription = "الحذاء الأبيض الأكثر شهرة في العالم بجلد طبيعي ناعم وتوسيد Nike Air المريح للغاية.",
                        Description = "يمنحك حذاء Nike Air Force 1 إطلالة أنيقة ونظيفة تتماشى مع كل الملابس، مع متانة الجلد الأصلي ونعل مطاطي بنمط دائري كلاسيكي.",
                        BasePrice = 115m, CostPrice = 65m, CompareAtPrice = 130m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 50, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-shoes", "nike",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?w=800&auto=format&fit=crop&q=80", true, "حذاء نايكي إير فورس 1 أبيض")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("مقاس 38", "NKE-AF1-38", 115m, 130m, new[] { ("المقاس", "SIZE", "38") }),
                        ("مقاس 39", "NKE-AF1-39", 115m, 130m, new[] { ("المقاس", "SIZE", "39") })
                    },
                    55
                ),

                // ==========================================
                // 13. WOMEN'S BAGS
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حقيبة ظهر ذكية مضادة للماء والسرقة مع منفذ شحن USB",
                        Slug = "smart-anti-theft-laptop-backpack",
                        Sku = "BAG-SMART-ANTITHEFT",
                        ShortDescription = "حقيبة لابتوب متطورة بقفل أمان وسحابات مخفية وخامة مقاومة للمطر والخدوش وسعة 35 لتر.",
                        Description = "الحقيبة المثالية للسفر والعمل والجامعة. تتسع لحاسوب محمول حتى 15.6 إنش مع جيوب مبطنة متعددة لحماية الأجهزة ومنفذ USB مدمج.",
                        BasePrice = 49m, CostPrice = 22m, CompareAtPrice = 69m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.6m, ReviewCount = 31, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-bags", null,
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&auto=format&fit=crop&q=80", true, "حقيبة ظهر ذكية سوداء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    100
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "حقيبة يد نسائية من الجلد الطبيعي الفاخر هيشي (Heshe Leather Tote Bag)",
                        Slug = "heshe-luxury-leather-tote-bag",
                        Sku = "HSH-TOTE-LTHR",
                        ShortDescription = "حقيبة كتف ويد أنيقة مصنوعة من جلد البقر الأصلي عالي الجودة مع حزام قابل للتعديل وسحاب متين.",
                        Description = "تجمع حقيبة Heshe الجلدية بين الفخامة والعملية اليومية، وتتسع لجميع متعلقاتك مع جيوب داخلية مبطنة وفاخرة.",
                        BasePrice = 95m, CostPrice = 50m, CompareAtPrice = 125m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 17, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-bags", "heshe",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1584917865442-de89df76afd3?w=800&auto=format&fit=crop&q=80", true, "حقيبة جلدية فاخرة هيشي")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("بني كلاسيكي", "HSH-TOTE-BRN", 95m, 125m, new[] { ("اللون", "COLOR", "بني كلاسيكي") }),
                        ("أسود ملكي", "HSH-TOTE-BLK", 95m, 125m, new[] { ("اللون", "COLOR", "أسود ملكي") })
                    },
                    45
                ),

                // ==========================================
                // 14. KITCHEN TOOLS
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ماكينة إعداد الإسبريسو الإيطالية ديلونجي ديديكا (De'Longhi Dedica)",
                        Slug = "delonghi-dedica-espresso-machine",
                        Sku = "DLG-DEDICA-EC685",
                        ShortDescription = "ماكينة قهوة أنيقة ومضغوطة بضغط 15 بار ونظام تسخين سريع وعصا تبخير الحليب الاحترافية.",
                        Description = "استمتع بكوب قهوة إسبريسو وكابتشينو مثالي بجودة المقاهي الإيطالية في راحة منزلك بعرض 15 سم فقط يناسب أي مطبخ مع نظام تسخين Thermoblock.",
                        BasePrice = 279m, CostPrice = 170m, CompareAtPrice = 320m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 17, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "kitchen-tools", "delonghi",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", true, "ماكينة قهوة ديلونجي ديديكا")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    30
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ماكينة قهوة نسبريسو فيرتو بوب الذكية (Nespresso Vertuo Pop)",
                        Slug = "nespresso-vertuo-pop-machine",
                        Sku = "NSP-VERTUO-POP",
                        ShortDescription = "ماكينة قهوة أنيقة ومضغوطة بتقنية الطرد المركزي Centrifusion لتحضير 4 أحجام مختلفة من الأكواب.",
                        Description = "أضف لمسة من البهجة لمطبخك مع ماكينة Nespresso Vertuo Pop. تقرأ الماكينة الرمز الشريطي لكل كبسولة لتعديل معايير الاستخلاص تلقائياً وتقديم فنجان قهوة كريمي غني.",
                        BasePrice = 129m, CostPrice = 75m, CompareAtPrice = 159m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 26, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "kitchen-tools", "nespresso",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", true, "ماكينة قهوة نسبريسو فيرتو بوب")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود كلاسيكي", "NSP-VPOP-BLK", 129m, 159m, new[] { ("اللون", "COLOR", "أسود كلاسيكي") }),
                        ("أحمر ناري", "NSP-VPOP-RED", 129m, 159m, new[] { ("اللون", "COLOR", "أحمر ناري") })
                    },
                    40
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "قلاية فيليبس الهوائية الذكية XXL سعة 7.3 لتر (Philips Airfryer XXL)",
                        Slug = "philips-airfryer-xxl-smart",
                        Sku = "PHL-AIRFRYER-XXL",
                        ShortDescription = "قلاية هوائية عائلية بتقنية إزالة الدهون Rapid Air وبرامج طهي ذكية بلمسة واحدة.",
                        Description = "حضّر أشهى الوجبات الصحية المقرمشة لعائلتك مع قلاية Philips Airfryer XXL بدهون أقل بنسبة تصل إلى 90% مع تقنية استشعار ذكية تضبط الوقت ودرجة الحرارة تلقائياً.",
                        BasePrice = 299m, CostPrice = 185m, CompareAtPrice = 349m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 39, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "kitchen-tools", "philips",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1585338107529-13afc5f02586?w=800&auto=format&fit=crop&q=80", true, "قلاية هوائية ذكية فيليبس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    35
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "خلاط يدوي متعدد الاستخدامات بوش إرجوميكس 1000 واط (Bosch ErgoMixx)",
                        Slug = "bosch-ergomixx-hand-blender-1000w",
                        Sku = "BSH-ERGOMIXX-1K",
                        ShortDescription = "خلاط يدوي ألماني بقوة 1000W مع شفرات QuattroBlade ومفرمة ومضرب خفق و12 سرعة مختلفة.",
                        Description = "مساعدك الأقوى في المطبخ لتحضير الشوربات والصلصات وفرم اللحوم والخضار وعجن المخبوزات الخفيفة بنقرة زر واحدة مع شفرات حادة من الستانلس ستيل.",
                        BasePrice = 89m, CostPrice = 48m, CompareAtPrice = 109m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 20, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "kitchen-tools", "bosch",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=800&auto=format&fit=crop&q=80", true, "خلاط يدوي بوش إرجوميكس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),

                // ==========================================
                // 15. HOME DECOR & FURNITURE
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "كرسي مكتب مريح آيكيا ماركوس (IKEA Markus Ergonomic Office Chair)",
                        Slug = "ikea-markus-office-chair",
                        Sku = "IKA-MARKUS-CHR",
                        ShortDescription = "كرسي العمل المكتبي الأكثر شهرة في العالم مع مسند ظهر شبكي داعم للفقرات وتعديل الارتفاع والميلان.",
                        Description = "صُمم كرسي IKEA Markus ليمنحك راحة تامة أثناء ساعات العمل والدراسة الطويلة، مع نسيج شبكي يسمح بمرور الهواء وآلية إمالة متزامنة تدعم ظهرك بالكامل.",
                        BasePrice = 199m, CostPrice = 110m, CompareAtPrice = 249m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 44, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "furniture", "ikea",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1580481077197-2a543322d7c0?w=800&auto=format&fit=crop&q=80", true, "كرسي مكتب آيكيا ماركوس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("رمادي غامق جيسفال", "IKA-MRK-GRY", 199m, 249m, new[] { ("اللون", "COLOR", "رمادي غامق") }),
                        ("أسود فحم", "IKA-MRK-BLK", 199m, 249m, new[] { ("اللون", "COLOR", "أسود فحم") })
                    },
                    30
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "كرسي بارسلونا الجلدي الأيقوني نول (Knoll Barcelona Chair)",
                        Slug = "knoll-barcelona-designer-chair",
                        Sku = "KNL-BRCLNA-CHR",
                        ShortDescription = "تحفة الأثاث المعماري الكلاسيكي المصنوعة يدوياً من أفخم أنواع الجلد الطبيعي وإطار الفولاذ المصقول.",
                        Description = "يعد كرسي Barcelona من تصاميم ميس فان دير روه أيقونة في عالم التصميم الحديث، حيث يجمع بين التناسب المعماري المثالي والراحة الفاخرة المطلقة.",
                        BasePrice = 1899m, CostPrice = 1250m, CompareAtPrice = 2200m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 9, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "furniture", "knoll",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&auto=format&fit=crop&q=80", true, "كرسي بارسلونا نول")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    10
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "طاولة طعام إيطالية فاخرة من خشب الجوز أنيبالي كولومبو (Annibale Colombo Dining Table)",
                        Slug = "annibale-colombo-luxury-dining-table",
                        Sku = "ANC-TABLE-WLNT",
                        ShortDescription = "طاولة طعام يدوية الصنع تتسع لـ 10 أشخاص مصنوعة من خشب الجوز الإيطالي الصلب مع تطعيمات فنية.",
                        Description = "تجسيد الفن الإيطالي العريق في النجارة الكلاسيكية من دار Annibale Colombo مع تشطيب نهائي يدوي بالشمع الطبيعي المقاوم للخدوش.",
                        BasePrice = 3499m, CostPrice = 2300m, CompareAtPrice = 3999m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 6, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "furniture", "annibale-colombo",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1615066390971-03e4e1c36ddf?w=800&auto=format&fit=crop&q=80", true, "طاولة طعام خشب جوز إيطالية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    6
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "مصباح طاولة زجاجي كروي آيكيا فادو (IKEA Fado Table Lamp)",
                        Slug = "ikea-fado-table-lamp",
                        Sku = "IKA-FADO-LMP",
                        ShortDescription = "مصباح طاولة كروي زجاجي يمنح الغرفة إضاءة دافئة وناعمة وخافتة للاسترخاء والقراءة.",
                        Description = "يتميز مصباح IKEA Fado بتصميم دائري بسيط يناسب غرف النوم وغرف المعيشة مع زجاج معالج لتوزيع متجانس للضوء.",
                        BasePrice = 25m, CostPrice = 12m, CompareAtPrice = 35m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 29, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "home-decor", "ikea",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&auto=format&fit=crop&q=80", true, "مصباح طاولة كروي فادو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    50
                ),

                // ==========================================
                // 16. PERFUMES & FRAGRANCES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر سوفاج ديور أو دو بارفان (Dior Sauvage EDP)",
                        Slug = "dior-sauvage-edp",
                        Sku = "DIOR-SAUVAGE-EDP",
                        ShortDescription = "عطر رجالي شرقي منعش وجريء يمزج نفحات البرغموت الكالابري مع خشب الصندل والفانيليا الجذابة.",
                        Description = "عطر سوفاج أو دو بارفان من دار ديور الفرنسية هو تحفة عطرية مستوحاة من سحر الصحراء في ساعة الغسق مع قاعدة غنية من الفانيليا وخشب الصندل التي تدوم طويلاً.",
                        BasePrice = 145m, CostPrice = 85m, CompareAtPrice = 165m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 42, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "dior",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1541643600914-78b084683601?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر سوفاج ديور")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("حجم 100 مل", "DIOR-SVG-100ML", 145m, 165m, new[] { ("الحجم", "VOLUME", "100 مل") }),
                        ("حجم 200 مل", "DIOR-SVG-200ML", 210m, 240m, new[] { ("الحجم", "VOLUME", "200 مل") })
                    },
                    70
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر ميس ديور أو دو بارفان النسائي الفاخر (Miss Dior EDP)",
                        Slug = "miss-dior-eau-de-parfum",
                        Sku = "DIOR-MISSDIOR-EDP",
                        ShortDescription = "عطر نسائي زهري ساحر يفيض بنفحات الورد الجوري والفاوانيا والسوسن مع لمسات الفانيليا الرقيقة.",
                        Description = "عطر Miss Dior Eau de Parfum هو باقة زهرية مفعمة بالحيوية والحياة، تتماوج روائح زهور سنتيفوليا مع الورد الدمشقي والبرغموت المنعش لتعبر عن الأنوثة الراقية.",
                        BasePrice = 135m, CostPrice = 80m, CompareAtPrice = 155m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 33, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "dior",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر ميس ديور")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("حجم 50 مل", "DIOR-MD-50ML", 135m, 155m, new[] { ("الحجم", "VOLUME", "50 مل") }),
                        ("حجم 100 مل", "DIOR-MD-100ML", 175m, 195m, new[] { ("الحجم", "VOLUME", "100 مل") })
                    },
                    60
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر كالفن كلاين سي كيه ون للجنسين (Calvin Klein CK One EDT)",
                        Slug = "calvin-klein-ck-one-edt",
                        Sku = "CK-ONE-EDT",
                        ShortDescription = "العطر الأيقوني المنعش للجنسين بنفحات الشاي الأخضر والبابايا والبرغموت والمسك.",
                        Description = "عطر CK One من كالفن كلاين يجسد روح الحرية والنقاء بتوليفة فريدة تناسب الرجال والنساء على حد سواء مع افتتاحية منعشة من الحمضيات وقاعدة عنبرية دافئة.",
                        BasePrice = 65m, CostPrice = 35m, CompareAtPrice = 85m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 28, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "calvin-klein",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر كالفن كلاين سي كيه ون")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("حجم 100 مل", "CK-ONE-100ML", 65m, 85m, new[] { ("الحجم", "VOLUME", "100 مل") })
                    },
                    75
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر بلو دي شانيل أو دو بارفان (Bleu de Chanel EDP)",
                        Slug = "bleu-de-chanel-edp",
                        Sku = "CHN-BLEU-EDP",
                        ShortDescription = "عطر أسطوري للرجل الواثق بنفحات أخشاب الأرز العطرية وخشب الصندل الجديد من كاليدونيا.",
                        Description = "يعبر عطر Bleu de Chanel عن الإنجاز والأناقة الخالدة بتوقيع عطري نقي وعميق يترك هيبة استثنائية تدوم طوال اليوم.",
                        BasePrice = 165m, CostPrice = 100m, CompareAtPrice = 185m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 48, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "chanel",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر بلو دي شانيل")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("حجم 100 مل", "CHN-BLEU-100ML", 165m, 185m, new[] { ("الحجم", "VOLUME", "100 مل") })
                    },
                    60
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر غوتشي فلورا جورجوس جاردينيا (Gucci Flora Gorgeous Gardenia)",
                        Slug = "gucci-flora-gorgeous-gardenia-edp",
                        Sku = "GCC-FLORA-GG",
                        ShortDescription = "عطر نسائي بهيج يجمع عبير الجاردينيا البيضاء مع الياسمين الشمسي وزهر الكمثرى الحلو.",
                        Description = "قصيدة غنائية مبهجة للزهور من دار غوتشي في زجاجة وردية مصممة بنمط فلورا التاريخي المستوحى من ألوان الطبيعة.",
                        BasePrice = 145m, CostPrice = 88m, CompareAtPrice = 165m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 26, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "gucci",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800&auto=format&fit=crop&q=80", true, "عطر غوتشي فلورا")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر برادا بارادوكس أو دو بارفان (Prada Paradoxe EDP)",
                        Slug = "prada-paradoxe-eau-de-parfum",
                        Sku = "PRD-PARADOXE-EDP",
                        ShortDescription = "عطر زهري عنبري يعيد اكتشاف التناقضات العطرية بزهر البرتقال والعنبر الحيوي وجزيء المسك الثوري.",
                        Description = "زجاجة أيقونية على شكل مثلث برادا الشهير، تعبر عن التجدد الدائم والأنوثة العصرية العميقة.",
                        BasePrice = 150m, CostPrice = 90m, CompareAtPrice = 170m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 21, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "prada",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1541643600914-78b084683601?w=800&auto=format&fit=crop&q=80", true, "عطر برادا بارادوكس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    35
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عطر دولتشي آند غابانا لايت بلو للرجال (Dolce & Gabbana Light Blue Pour Homme)",
                        Slug = "dolce-gabbana-light-blue-pour-homme",
                        Sku = "DG-LIGHTBLUE-MEN",
                        ShortDescription = "عطر حمضي منعش يجسد سحر البحر الأبيض المتوسط بالعرعر والبرغموت المنعش والفلفل السيشواني.",
                        Description = "العطر الصيفي المنعش الخالد من Dolce & Gabbana الذي ينقلك إلى شواطئ كابري الإيطالية بنفحاته المنعشة وقاعدته من خشب المسك.",
                        BasePrice = 85m, CostPrice = 48m, CompareAtPrice = 105m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 34, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "perfumes-fragrances", "dolce-gabbana",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&auto=format&fit=crop&q=80", true, "عطر دولتشي آند غابانا لايت بلو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    50
                ),

                // ==========================================
                // 17. SKINCARE
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "سيروم لوريال ريفايتلاليفت بحمض الهيالورونيك 1.5% (L'Oréal Revitalift)",
                        Slug = "loreal-revitalift-hyaluronic-acid-serum",
                        Sku = "LOR-REVIT-HA-30",
                        ShortDescription = "سيروم مكثف لترطيب البشرة واستعادة امتلائها وتقليل التجاعيد بنسبة 1.5% حمض الهيالورونيك النقي.",
                        Description = "يعد سيروم لوريال ريفايتلاليفت بحمض الهيالورونيك النقي الحل المثالي لبشرة نضرة ومشدودة. تركيبة خفيفة وسريعة الامتصاص تتغلغل بعمق لترطيب فوري.",
                        BasePrice = 29m, CostPrice = 14m, CompareAtPrice = 39m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 54, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "skincare", "loreal",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&auto=format&fit=crop&q=80", true, "سيروم لوريال ريفايتلاليفت")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("سعة 30 مل", "LOR-HA-30ML", 29m, 39m, new[] { ("الحجم", "VOLUME", "30 مل") }),
                        ("سعة 50 مل", "LOR-HA-50ML", 42m, 55m, new[] { ("الحجم", "VOLUME", "50 مل") })
                    },
                    110
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "كريم أولاي ريجينيريست لمكافحة التجاعيد ونحت الوجه (Olay Regenerist Micro-Sculpting)",
                        Slug = "olay-regenerist-micro-sculpting-cream",
                        Sku = "OLY-REGEN-50G",
                        ShortDescription = "كريم الترطيب المتقدم بمركب الأمينو ببتيد وحمض الهيالورونيك وفيتامين B3 لشد وتجديد خلايا البشرة.",
                        Description = "يمنحك كريم Olay Regenerist ترطيباً عميقاً يستمر 24 ساعة مع تقليل ملحوظ للخطوط الدقيقة والتجاعيد خلال 28 يوماً فقط.",
                        BasePrice = 35m, CostPrice = 18m, CompareAtPrice = 45m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.7m, ReviewCount = 38, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "skincare", "olay",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&auto=format&fit=crop&q=80", true, "كريم أولاي ريجينيريست")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    75
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "زبدة الجسم المرطبة بزبدة الكاكاو النقية فازلين (Vaseline Cocoa Radiant Body Butter)",
                        Slug = "vaseline-cocoa-radiant-body-butter",
                        Sku = "VSL-COCOA-250G",
                        ShortDescription = "مرطب غني بزبدة الكاكاو النقية 100% وزبدة الشيا لترطيب فائق ونضارة طبيعية للبشرة الجافة.",
                        Description = "تساعد زبدة الجسم Vaseline Intensive Care على حبس الرطوبة داخل طبقات الجلد الجاف لتمنحه نعومة حريرية ولمعاناً صحياً برائحة الكاكاو الدافئة.",
                        BasePrice = 12m, CostPrice = 6m, CompareAtPrice = 18m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 62, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "skincare", "vaseline",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&auto=format&fit=crop&q=80", true, "زبدة الجسم فازلين كاكاو")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    150
                ),

                // ==========================================
                // 18. MAKEUP
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ماسكارا إيسنس لاش برنسس لتأثير الرموش الاصطناعية (Essence Lash Princess Mascara)",
                        Slug = "essence-lash-princess-false-lash-mascara",
                        Sku = "ESS-LASHP-MASC",
                        ShortDescription = "الماسكارا الأكثر شهرة لتكثيف وتطويل الرموش بشكل دراماتيكي مذهل بدون تكتل.",
                        Description = "تمنحك فرشاة الألياف الخاصة المخروطية الشكل رموشاً طويلة وكثيفة ومحددة بضربة واحدة تدوم طوال اليوم بثبات ممتاز وسواد فاحم.",
                        BasePrice = 9m, CostPrice = 4m, CompareAtPrice = 14m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 85, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "makeup", "essence",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&auto=format&fit=crop&q=80", true, "ماسكارا إيسنس لاش برنسس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    200
                ),

                // ==========================================
                // 19. PERSONAL CARE
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ماكينة حلاقة كهربائية ذكية فيليبس سلسلة 9000 (Philips Series 9000)",
                        Slug = "philips-series-9000-shaver",
                        Sku = "PHL-SHAVER-S9000",
                        ShortDescription = "ماكينة الحلاقة الأكثر تطوراً بتقنية الذكاء الاصطناعي SkinIQ ورؤوس مرنة تدور في 360 درجة.",
                        Description = "توفر ماكينة الحلاقة Philips Series 9000 حلاقة فائقة النعومة مع حماية قصوى للبشرة، حيث تستشعر كثافة اللحية 500 مرة في الثانية وتتكيف تلقائياً.",
                        BasePrice = 229m, CostPrice = 140m, CompareAtPrice = 269m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 25, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "personal-care", "philips",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1621607512214-68297480165e?w=800&auto=format&fit=crop&q=80", true, "ماكينة حلاقة فيليبس")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "جل استحمام طبيعي نباتي بخلاصة أوراق الزيتون أتيتيود (Attitude Shower Gel 473ml)",
                        Slug = "attitude-natural-shower-gel-olive",
                        Sku = "ATT-SHWRGEL-OLV",
                        ShortDescription = "غسول جسم طبيعي 100% غني بمضادات الأكسدة وخالٍ تماماً من الكبريتات والبارابين معتمد من EWG.",
                        Description = "يغذي بشرتك بعناية فائقة وينظفها بلطف مع الحفاظ على حاجز الرطوبة الطبيعي بفضل مستخلصات أوراق الزيتون والشاي الأبيض.",
                        BasePrice = 15m, CostPrice = 7m, CompareAtPrice = 20m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 18, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "personal-care", "attitude",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&auto=format&fit=crop&q=80", true, "جل استحمام طبيعي أتيتيود")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    80
                ),

                // ==========================================
                // 20. MEN'S WATCHES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة أبل الذكية الجيل التاسع (Apple Watch Series 9 GPS)",
                        Slug = "apple-watch-series-9",
                        Sku = "APL-WATCH-S9",
                        ShortDescription = "ساعة ذكية بشريحة S9 فائقة السرعة وإيماءة الضغط المزدوج المبتكرة ومستشعرات صحية متقدمة.",
                        Description = "تأتي ساعة Apple Watch Series 9 بقوة شريحة S9 SiP المخصصة من أبل مع شاشة فائقة السطوع تصل إلى 2000 شمعة ومراقبة متواصلة للصحة والتمارين.",
                        BasePrice = 399m, CostPrice = 280m, CompareAtPrice = 429m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 29, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "apple",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", true, "ساعة يد ذكية أبل")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("هيكل ألمنيوم 45 مم - سماء الليل", "APL-W-S9-45-MID", 429m, 459m, new[] { ("المقاس", "SIZE", "45 مم"), ("اللون", "COLOR", "سماء الليل") })
                    },
                    35
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة كاسيو جي شوك المقاومة للصدمات GA-2100 (Casio G-Shock)",
                        Slug = "casio-g-shock-ga-2100",
                        Sku = "CSO-GSHOCK-GA2100",
                        ShortDescription = "ساعة جي شوك الأيقونية بهيكل الكربون القوي وتصميم ثماني الأضلاع نحيف ومقاومة للماء 200 متر.",
                        Description = "تتميز ساعة Casio G-Shock GA-2100 الشهيرة بلقب 'كاسيو أوك' بهيكل نحيف متين مدعم بألياف الكربون Carbon Core Guard ومقاومة تامة للصدمات والمياه.",
                        BasePrice = 110m, CostPrice = 65m, CompareAtPrice = 130m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 37, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "casio",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80", true, "ساعة كاسيو جي شوك سوداء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("أسود بالكامل (All Black)", "CSO-GA2100-BLK", 110m, 130m, new[] { ("اللون", "COLOR", "أسود بالكامل") })
                    },
                    55
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة رولكس سابمارينر ديت الأوتوماتيكية الفاخرة (Rolex Submariner Date)",
                        Slug = "rolex-submariner-date-luxury-watch",
                        Sku = "RLX-SUB-DATE-41",
                        ShortDescription = "ساعة الغواصين الفاخرة المصنوعة من فولاذ أويستر ستيل Oystersteel مع إطار سيراميك Cerachrom الأسود.",
                        Description = "تعتبر Rolex Submariner المرجع المطلق لساعات الغوص الاحترافية الفاخرة مع حركة كاليبر 3235 الأوتوماتيكية ومقاومة للماء حتى 300 متر.",
                        BasePrice = 11900m, CostPrice = 9000m, CompareAtPrice = 13500m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 15, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "rolex",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=80", true, "ساعة رولكس سابمارينر ديت")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    5
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة لونجين هايدرو كونكويست الأوتوماتيكية 41 مم (Longines HydroConquest)",
                        Slug = "longines-hydroconquest-automatic-watch",
                        Sku = "LNG-HCQ-41-BLU",
                        ShortDescription = "ساعة غوص سويسرية متميزة بإطار سيراميك أزرق مقاوم للخدش وحركة أوتوماتيكية باحتياطي طاقة 72 ساعة.",
                        Description = "تجمع ساعة Longines HydroConquest بين التميز التقني والأناقة الرياضية الراقية لمقاومة الماء حتى 30 بار (300 متر).",
                        BasePrice = 1450m, CostPrice = 980m, CompareAtPrice = 1650m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 14, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "longines",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80", true, "ساعة لونجين سويسرية زرقاء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    12
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة آي دبليو سي بورتوجيزر كرونوغراف الفاخرة (IWC Portugieser Chronograph)",
                        Slug = "iwc-portugieser-chronograph-luxury",
                        Sku = "IWC-PORT-CHRONO",
                        ShortDescription = "ساعة كلاسيكية راقية بميناء أبيض نقي وأرقام عربية زرقاء وحركة كرونوغراف عيار 69355 المصنعة داخلياً.",
                        Description = "تعتبر IWC Portugieser واحدة من أشهر أيقونات الساعات السويسرية الفاخرة مع سوار من جلد التمساح الأسود الأنيق.",
                        BasePrice = 7900m, CostPrice = 5800m, CompareAtPrice = 8800m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 8, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "iwc",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", true, "ساعة آي دبليو سي بورتوجيزر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    6
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة هواوي الذكية جي تي 4 قياس 46 مم (Huawei Watch GT 4)",
                        Slug = "huawei-watch-gt-4-smartwatch",
                        Sku = "HWI-WGT4-46MM",
                        ShortDescription = "ساعة ذكية أنيقة بهيكل هندسي ثماني الأضلاع وبطارية تدوم حتى 14 يوماً وتتبع متقدم للصحة والرياضة.",
                        Description = "تأتي Huawei Watch GT 4 بشاشة AMOLED مذهلة مقاس 1.43 بوصة مع إدارة السعرات الحرارية وإدارة متقدمة للنوم ومقاومة للماء 5ATM.",
                        BasePrice = 229m, CostPrice = 150m, CompareAtPrice = 269m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 27, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "mens-watches", "huawei",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=800&auto=format&fit=crop&q=80", true, "ساعة هواوي الذكية جي تي 4")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("حزام جلدي بني", "HWI-GT4-BRN", 249m, 289m, new[] { ("اللون", "COLOR", "حزام جلدي بني") }),
                        ("حزام أسود رياضي", "HWI-GT4-BLK", 229m, 269m, new[] { ("اللون", "COLOR", "حزام أسود رياضي") })
                    },
                    35
                ),

                // ==========================================
                // 21. WOMEN'S WATCHES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "ساعة كاسيو فينتاج الرقمية الذهبية النسائية (Casio Vintage Gold Watch)",
                        Slug = "casio-vintage-gold-digital-watch",
                        Sku = "CSO-VINTAGE-GLD",
                        ShortDescription = "ساعة كاسيو فينتاج الأيقونية المطلية بالذهب مع شاشة رقمية وساعة إيقاف ومنبه ومقاومة للماء.",
                        Description = "تصميم فينتاج ريترو جذاب ومحبوب يناسب الإطلالات الكاجوال والرسمية بحجم مناسب لمعصم السيدات.",
                        BasePrice = 45m, CostPrice = 22m, CompareAtPrice = 60m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 40, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "womens-watches", "casio",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80", true, "ساعة كاسيو فينتاج ذهبية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    60
                ),

                // ==========================================
                // 22. SUNGLASSES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "نظارة شمسية ريبان أفياتور الكلاسيكية الأصلية (Ray-Ban Aviator Classic)",
                        Slug = "ray-ban-aviator-classic-sunglasses",
                        Sku = "RB-AVIATOR-3025",
                        ShortDescription = "النظارة الشمسية الأكثر شهرة في التاريخ بإطار معدني ذهبي وعدسات G-15 الأيقونية لحماية 100% من UV.",
                        Description = "صُممت نظارات Ray-Ban Aviator Classic في الأصل للطيارين الأمريكيين عام 1937 وتعتبر اليوم رمزاً للأناقة والجودة التي لا تبطل موضتها.",
                        BasePrice = 165m, CostPrice = 90m, CompareAtPrice = 185m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 30, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "sunglasses", "ray-ban",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1511499767150-a48a237f0083?w=800&auto=format&fit=crop&q=80", true, "نظارة شمسية ريبان أفياتور")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])> {
                        ("إطار ذهبي - عدسات خضراء G-15", "RB-3025-GLD-G15", 165m, 185m, new[] { ("اللون", "COLOR", "إطار ذهبي / أخضر G-15") }),
                        ("إطار أسود - عدسات مستقطبة Polarized", "RB-3025-BLK-POL", 195m, 215m, new[] { ("اللون", "COLOR", "إطار أسود / مستقطب") })
                    },
                    50
                ),

                // ==========================================
                // 23. JEWELLERY
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "عقد وسلسال الكريستال النقي إنفينيتي اللامع (Crystal Infinity Pendant)",
                        Slug = "crystal-infinity-pendant-necklace",
                        Sku = "JWL-INFN-PDNT",
                        ShortDescription = "عقد فاخر مطلي بالروديوم اللامع مع رمز اللانهاية المرصع بأحجار الكريستال الساحرة.",
                        Description = "هدية استثنائية معبرة عن الحب والأناقة الخالدة بتصميم رقيق يتلألأ مع كل حركة ويناسب جميع المناسبات.",
                        BasePrice = 85m, CostPrice = 38m, CompareAtPrice = 110m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 25, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "jewellery", null,
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=800&auto=format&fit=crop&q=80", true, "عقد كريستال فاخر")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),

                // ==========================================
                // 24. SPORTS EQUIPMENT
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "مجموعة أثقال دمبلز ذكية قابلة للتعديل حتى 24 كجم للتمارين المنزلية",
                        Slug = "adjustable-dumbbell-set-24kg",
                        Sku = "SPT-DUMBBELL-24KG",
                        ShortDescription = "دمبل ذكي يوفر 15 وزناً مختلفاً في أداة واحدة من 2.5 كجم إلى 24 كجم بنظام قفل أمان سريع.",
                        Description = "وداعاً للازدحام في الصالة الرياضية! يوفر لك هذا الدمبل تجربة تمرين شاملة لجميع عضلات الجسم بنقرة واحدة من القرص الدوار.",
                        BasePrice = 199m, CostPrice = 120m, CompareAtPrice = 249m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 14, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "sports-equipment", null,
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", true, "دمبل تمارين رياضية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    40
                ),

                // ==========================================
                // 25. GROCERIES & BEVERAGES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "قهوة نسكافيه جولد سريعة التحضير غنية وناعمة 200 جرام (Nescafé Gold)",
                        Slug = "nescafe-gold-instant-coffee-200g",
                        Sku = "NSC-GOLD-200G",
                        ShortDescription = "قهوة سريعة التحضير ممتازة بحبيبات البن المحمصة بعناية مع رائحة زكية وطعم غني متوازن.",
                        Description = "استمتع بلحظات القهوة الفريدة مع نسكافيه جولد المحضرة من أجود أنواع حبوب الأرابيكا والروبوستا المطحونة ناعماً.",
                        BasePrice = 14m, CostPrice = 8m, CompareAtPrice = 19m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 58, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "beverages", "nescafe",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=800&auto=format&fit=crop&q=80", true, "برطمان قهوة نسكافيه جولد")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    120
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "زيت زيتون فلسطيني بكر ممتاز عصرة أولى على البارد 1 لتر",
                        Slug = "palestinian-extra-virgin-olive-oil-1l",
                        Sku = "PAL-OLIVEOIL-1L",
                        ShortDescription = "زيت زيتون بلدي نقي 100% من أشجار الزيتون الرومي المعمرة بجبال فلسطين بنسبة حموضة أقل من 0.8%.",
                        Description = "زيت زيتون أصيل عصرة أولى على البارد بدون أي إضافات بطعم أخضر غني وفوائد صحية استثنائية لصحة القلب والجسم.",
                        BasePrice = 18m, CostPrice = 10m, CompareAtPrice = 24m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 5.0m, ReviewCount = 47, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "pantry-staples", null,
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=800&auto=format&fit=crop&q=80", true, "زيت زيتون بكر ممتاز")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    90
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "فراولة طازجة بلدية ممتازة 500 جرام (Fresh Strawberries)",
                        Slug = "fresh-strawberries-500g",
                        Sku = "GRO-STRAWBERRY-500G",
                        ShortDescription = "فراولة طازجة حلوة المذاق منتقاة بعناية يومياً من المزارع المحلية ومغلفة بأعلى معايير الجودة.",
                        Description = "فواكه طازجة وغنية بفيتامين C ومضادات الأكسدة مثالية للوجبات الخفيفة والحلويات والعصائر الطبيعية.",
                        BasePrice = 5m, CostPrice = 2m, CompareAtPrice = 7m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 31, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "fresh-produce", null,
                    new List<(string, bool, string)> {
                        ("https://cdn.dummyjson.com/product-images/groceries/strawberry/1.webp", true, "فراولة طازجة حمراء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    50
                ),

                // ==========================================
                // 26. CARS & MOTORCYCLES
                // ==========================================
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "دودج تشارجر SXT بنظام الدفع الخلفي موديل 2024 (Dodge Charger SXT RWD)",
                        Slug = "dodge-charger-sxt-rwd-2024",
                        Sku = "DDG-CHRGR-2024",
                        ShortDescription = "سيدان عضلات رياضية بمحرك Pentastar V6 سعة 3.6 لتر وقوة 292 حصاناً مع شاشة Uconnect 8.4.",
                        Description = "تجمع Dodge Charger بين هيبة سيارات العضلات والراحة العائلية مع ناقل حركة أوتوماتيكي TorqueFlite بـ 8 سرعات ومقصورة داخلية رياضية فاخرة.",
                        BasePrice = 32500m, CostPrice = 28000m, CompareAtPrice = 35000m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 11, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "cars", "dodge",
                    new List<(string, bool, string)> {
                        ("https://cdn.dummyjson.com/product-images/vehicle/charger-sxt-rwd/1.webp", true, "سيارة دودج تشارجر سوداء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    4
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "كرايسلر 300C الفاخرة بمحرك هيمي 5.7 لتر V8 (Chrysler 300C)",
                        Slug = "chrysler-300c-hemi-v8-luxury-sedan",
                        Sku = "CHR-300C-V8",
                        ShortDescription = "سيدان تنفيذية فاخرة بمحرك HEMI V8 الأسطوري بقوة 363 حصاناً ومقاعد جلد نابا الفاخرة.",
                        Description = "الفخامة الأمريكية في أبهى صورها: نظام صوتي Harmon Kardon مع 19 مكبر صوت، وشبك أمامي مطلي بالكروم ومصابيح Bi-Xenon تكيفية.",
                        BasePrice = 38900m, CostPrice = 33000m, CompareAtPrice = 42000m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 9, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "cars", "chrysler",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&auto=format&fit=crop&q=80", true, "سيارة كرايسلر 300C فاخرة")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    3
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "دراجة نارية رياضية كاواساكي نينجا ZX-6R سعة 636 سي سي (Kawasaki Ninja ZX-6R)",
                        Slug = "kawasaki-ninja-zx-6r-supersport",
                        Sku = "KWK-NINJA-ZX6R",
                        ShortDescription = "دراجة سوبر سبورت بمحرك 4 أسطوانات 636cc ونظام تحكم في الجر KTRC ومساعد نقل السرعة السريع KQS.",
                        Description = "تحكم بحلبات السباق والشوارع مع Kawasaki Ninja ZX-6R، شاشة TFT ملونة جديدة مع اتصال بالهاتف الذكي وفرامل Nissin المزدوجة.",
                        BasePrice = 11399m, CostPrice = 9200m, CompareAtPrice = 12500m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = true, TrackInventory = true,
                        AverageRating = 4.9m, ReviewCount = 15, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "motorcycles", "kawasaki",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=800&auto=format&fit=crop&q=80", true, "دراجة كاواساكي نينجا خضراء")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    5
                ),
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "دراجة نارية هوندا CBR650R الرياضية (Honda CBR650R ABS)",
                        Slug = "honda-cbr650r-abs-sports-motorcycle",
                        Sku = "HND-CBR650R",
                        ShortDescription = "دراجة رياضية بمحرك 4 أسطوانات خطي 649cc ونظام القابض الإلكتروني الجديد Honda E-Clutch.",
                        Description = "تجمع Honda CBR650R بين إثارة القيادة الرياضية وسهولة الاستخدام اليومي بنظام تعليق Showa SFF-BP وتصميم مستوحى من دراجات Fireblade للبطولات.",
                        BasePrice = 9899m, CostPrice = 7800m, CompareAtPrice = 10800m, CurrencyCode = "USD",
                        IsActive = true, IsFeatured = false, TrackInventory = true,
                        AverageRating = 4.8m, ReviewCount = 12, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                    },
                    "motorcycles", "honda",
                    new List<(string, bool, string)> {
                        ("https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?w=800&auto=format&fit=crop&q=80", true, "دراجة نارية هوندا رياضية")
                    },
                    new List<(string, string, decimal, decimal, (string, string, string)[])>(),
                    6
                )
            };

            foreach (var item in productsToSeed)
            {
                var targetCatId = GetCatId(item.categorySlug);
                var targetBrandId = item.brandSlug != null ? GetBrandId(item.brandSlug) : null;

                var existingProduct = await db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.VariantAttributes)
                    .Include(p => p.InventoryItems)
                    .FirstOrDefaultAsync(p => p.Slug == item.product.Slug);

                if (existingProduct == null)
                {
                    var product = item.product;
                    product.CategoryId = targetCatId;
                    product.BrandId = targetBrandId;

                    await db.Products.AddAsync(product);
                    await db.SaveChangesAsync();

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

                        await SeedVariantAttributesAsync(db, variant.Id, v.options, attributeCache);

                        var varInv = new InventoryItem(product.Id, mainWarehouse.Id, item.stock / Math.Max(item.variants.Count, 1), variant.Id);
                        await db.InventoryItems.AddAsync(varInv);
                    }

                    var prodInv = new InventoryItem(product.Id, mainWarehouse.Id, item.stock);
                    await db.InventoryItems.AddAsync(prodInv);

                    await db.SaveChangesAsync();
                }
                else
                {
                    existingProduct.Name = item.product.Name;
                    existingProduct.Description = item.product.Description;
                    existingProduct.ShortDescription = item.product.ShortDescription;
                    existingProduct.BasePrice = item.product.BasePrice;
                    existingProduct.CompareAtPrice = item.product.CompareAtPrice;
                    existingProduct.CostPrice = item.product.CostPrice;
                    existingProduct.IsFeatured = item.product.IsFeatured;
                    existingProduct.IsActive = item.product.IsActive;
                    existingProduct.CategoryId = targetCatId;
                    existingProduct.BrandId = targetBrandId;
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

            _logger.LogInformation("Seeded and synchronized all {Count} products across subcategories and brands", productsToSeed.Count);
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
                    Comment = "الهاتف ممتاز جداً، خفة وزن التيتانيوم واضحة مقارنة بالإصدارات السابقة، والبطارية تدوم يوماً كاملاً والكاميرا 5X احترافية للغاية.",
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
            var electronicsCatIds = await db.Categories
                .Where(c => c.Slug == "electronics" || c.Slug == "smartphones" || c.Slug == "laptops" || c.Slug == "audio-headphones" || c.Slug == "gaming-consoles" || c.Slug == "tablets" || c.Slug == "tv-displays" || c.Slug == "phone-accessories" || c.Slug == "smart-home")
                .Select(c => c.Id.ToString())
                .ToListAsync();

            var fashionCatIds = await db.Categories
                .Where(c => c.Slug == "clothing-fashion" || c.Slug == "mens-clothing" || c.Slug == "womens-clothing" || c.Slug == "shoes-bags" || c.Slug == "mens-shoes" || c.Slug == "womens-shoes" || c.Slug == "womens-bags")
                .Select(c => c.Id.ToString())
                .ToListAsync();

            var beautyCatIds = await db.Categories
                .Where(c => c.Slug == "beauty-perfumes" || c.Slug == "perfumes-fragrances" || c.Slug == "skincare" || c.Slug == "makeup")
                .Select(c => c.Id.ToString())
                .ToListAsync();

            var seedPromotions = new List<Promotion>
            {
                new Promotion
                {
                    Id = Guid.NewGuid(),
                    Name = "عرض باقة الإلكترونيات والهواتف (Tech Festival 20%)",
                    Description = "خصم خاص 20% فوري يطبق تلقائياً على جميع الهواتف الذكية والحواسيب وملحقات الإلكترونيات.",
                    Type = "percentage",
                    RulesJson = "{\"discountPercentage\": 20}",
                    ApplicableCategoryIds = System.Text.Json.JsonSerializer.Serialize(electronicsCatIds),
                    Priority = 30,
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
                    Description = "اشتري قطعتين من الملابس والأزياء واحصل على القطعة الثالثة مجاناً 100%.",
                    Type = "buy_x_get_y",
                    RulesJson = "{\"buyQuantity\": 2, \"getQuantity\": 1, \"discountPercentage\": 100}",
                    ApplicableCategoryIds = System.Text.Json.JsonSerializer.Serialize(fashionCatIds),
                    Priority = 25,
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
                    Name = "عرض وفر 50 شيكل فوري على العطور (Save 50 ILS)",
                    Description = "خصم فوري بقيمة 50 ₪ يخصم تلقائياً عند شراء أفخم العطور ومستحضرات الجمال.",
                    Type = "fixed_amount",
                    RulesJson = "{\"discountAmount\": 50}",
                    ApplicableCategoryIds = System.Text.Json.JsonSerializer.Serialize(beautyCatIds),
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
                    Name = "خصم الصيف الكبير 15% (Summer Mega Sale)",
                    Description = "خصم فوري 15% على المنتجات المؤهلة في المتجر.",
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
                    Name = "خصومات السلة المتدرجة (Tiered Cart Discount)",
                    Description = "وفّر فوراً على مشترياتك حسب إجمالي السلة: وفّر 25 ₪ عند الشراء بـ 250 ₪ فأكثر، وفّر 60 ₪ عند الشراء بـ 500 ₪ فأكثر، ووفّر 150 ₪ عند الشراء بـ 1000 ₪ فأكثر.",
                    Type = "tiered_discount",
                    RulesJson = "{\"tiers\": [{\"minSpend\": 250, \"discount\": 25, \"discountType\": \"fixed_amount\"}, {\"minSpend\": 500, \"discount\": 60, \"discountType\": \"fixed_amount\"}, {\"minSpend\": 1000, \"discount\": 150, \"discountType\": \"fixed_amount\"}]}",
                    Priority = 1,
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
                var existing = await db.Promotions.FirstOrDefaultAsync(p => p.Name == promo.Name || p.Type == promo.Type && p.Type == "tiered_discount");
                if (existing == null)
                {
                    await db.Promotions.AddAsync(promo);
                }
                else
                {
                    existing.Name = promo.Name;
                    existing.Description = promo.Description;
                    existing.Type = promo.Type;
                    existing.RulesJson = promo.RulesJson;
                    existing.ApplicableCategoryIds = promo.ApplicableCategoryIds;
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

            if (iphone != null && appleWatch != null && sony != null)
            {
                var o1 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-001", CurrencyCode = "USD" };
                o1.AddItem(iphone.Id, iphone.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), iphone.Name, iphone.BasePrice, 1);
                o1.AddItem(appleWatch.Id, appleWatch.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), appleWatch.Name, appleWatch.BasePrice, 1);
                o1.AddItem(sony.Id, sony.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), sony.Name, sony.BasePrice, 1);
                orders.Add(o1);
            }

            if (iphone != null && appleWatch != null)
            {
                var o2 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-002", CurrencyCode = "USD" };
                o2.AddItem(iphone.Id, iphone.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), iphone.Name, iphone.BasePrice, 1);
                o2.AddItem(appleWatch.Id, appleWatch.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), appleWatch.Name, appleWatch.BasePrice, 1);
                orders.Add(o2);
            }

            if (dell != null && sony != null && backpack != null)
            {
                var o3 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-003", CurrencyCode = "USD" };
                o3.AddItem(dell.Id, dell.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), dell.Name, dell.BasePrice, 1);
                o3.AddItem(sony.Id, sony.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), sony.Name, sony.BasePrice, 1);
                o3.AddItem(backpack.Id, backpack.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), backpack.Name, backpack.BasePrice, 1);
                orders.Add(o3);
            }

            if (nike != null && adidas != null && dumbbells != null)
            {
                var o4 = new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-2026-004", CurrencyCode = "USD" };
                o4.AddItem(nike.Id, nike.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), nike.Name, nike.BasePrice, 1);
                o4.AddItem(adidas.Id, adidas.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), adidas.Name, adidas.BasePrice, 1);
                o4.AddItem(dumbbells.Id, dumbbells.Variants.FirstOrDefault()?.Id ?? Guid.NewGuid(), dumbbells.Name, dumbbells.BasePrice, 1);
                orders.Add(o4);
            }

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
