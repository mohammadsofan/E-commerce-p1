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
                await db.Database.EnsureCreatedAsync();

                await SeedRolesAsync(roleManager);
                await SeedCurrenciesAsync(db);
                await SeedWarehousesAsync(db);
                await SeedShippingZonesAsync(db);
                await SeedHeroBannersAsync(db);
                await SeedStoreFeaturesAsync(db);
                await SeedCategoriesAsync(db);
                await SeedBrandsAsync(db);
                await SeedProductsAsync(db);
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

        private async Task SeedCurrenciesAsync(ApplicationDbContext db)
        {
            if (await db.Currencies.AnyAsync()) return;

            var currencies = new List<Currency>
            {
                new Currency { Id = Guid.NewGuid(), Code = "USD", Symbol = "$", IsBaseCurrency = true },
                new Currency { Id = Guid.NewGuid(), Code = "ILS", Symbol = "₪", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "EUR", Symbol = "€", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "GBP", Symbol = "£", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "SAR", Symbol = "ر.س", IsBaseCurrency = false },
                new Currency { Id = Guid.NewGuid(), Code = "AED", Symbol = "د.إ", IsBaseCurrency = false },
            };

            await db.Currencies.AddRangeAsync(currencies);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} currencies", currencies.Count);
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
            if (await db.HeroBanners.AnyAsync()) return;

            var banner = new HeroBanner
            {
                Id = Guid.NewGuid(),
                BadgeText = "مجموعة جديدة 2024",
                Title = "اكتشف منتجات مذهلة بأسعار لا تُقاوم",
                Subtitle = "تسوق أحدث الصيحات في الإلكترونيات والأزياء والمنزل والمزيد. شحن مجاني للطلبات فوق ₪50. إرجاع سهل خلال 30 يوماً.",
                PrimaryButtonText = "تسوق الآن",
                PrimaryButtonLink = "/products",
                SecondaryButtonText = "تصفح التصنيفات",
                SecondaryButtonLink = "/categories",
                ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800&auto=format&fit=crop&q=80",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await db.HeroBanners.AddAsync(banner);
            await db.SaveChangesAsync();
            _logger.LogInformation("Seeded default hero banner");
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
                    Description = "أحدث الأجهزة الذكية والإلكترونيات الاستهلاكية وملحقاتها",
                    ImageUrl = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 1,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "إلكترونيات",
                    MetaDescription = "تسوق إلكترونيات",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "ملابس وأزياء",
                    Slug = "clothing-fashion",
                    Description = "أرقى صيحات الموضة والأزياء الرجالية والنسائية لجميع المناسبات",
                    ImageUrl = "https://images.unsplash.com/photo-1445205170230-053b83016050?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 2,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "ملابس وأزياء",
                    MetaDescription = "تسوق ملابس وأزياء",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "أحذية وحقائب",
                    Slug = "shoes-bags",
                    Description = "أحذية رياضية ورسمية وحقائب يد وسفر عالية الجودة",
                    ImageUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 3,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "أحذية وحقائب",
                    MetaDescription = "تسوق أحذية وحقائب",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "المنزل والمطبخ",
                    Slug = "home-kitchen",
                    Description = "أدوات منزلية ومستلزمات مطبخ وديكورات عصرية لمنزل مريح",
                    ImageUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 4,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "المنزل والمطبخ",
                    MetaDescription = "تسوق مستلزمات المنزل والمطبخ",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "العطور والجمال",
                    Slug = "beauty-perfumes",
                    Description = "أفخم العطور الأصلية ومستحضرات العناية بالبشرة والجمال",
                    ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 5,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "العطور والجمال",
                    MetaDescription = "تسوق العطور ومستحضرات الجمال",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الرياضة واللياقة",
                    Slug = "sports-fitness",
                    Description = "معدات التمارين الرياضية والملابس الرياضية المريحة",
                    ImageUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 6,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "الرياضة واللياقة",
                    MetaDescription = "تسوق مستلزمات الرياضة واللياقة",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الساعات والإكسسوارات",
                    Slug = "watches-accessories",
                    Description = "ساعات فاخرة وذكية وإكسسوارات أنيقة تناسب ذوقك",
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 7,
                    IsActive = true,
                    IsFeatured = false,
                    MetaTitle = "الساعات والإكسسوارات",
                    MetaDescription = "تسوق الساعات والإكسسوارات",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                },
                new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "الهواتف الذكية",
                    Slug = "smartphones",
                    Description = "أحدث الهواتف الذكية من كبرى العلامات العالمية مع إكسسواراتها",
                    ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&auto=format&fit=crop&q=80",
                    DisplayOrder = 8,
                    IsActive = true,
                    IsFeatured = true,
                    MetaTitle = "الهواتف الذكية",
                    MetaDescription = "تسوق الهواتف الذكية",
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
                    Description = "الشركة الرائدة عالمياً في الابتكار التكنولوجي والأجهزة الذكية",
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
                    Description = "تقنيات متطورة وشاشات مذهلة وأجهزة منزلية وهواتف ذكية رائدة",
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
                    Description = "العلامة الرياضية الأولى عالمياً للأحذية والملابس الرياضية المبتكرة",
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
                    Description = "تصاميم رياضية أيقونية وأداء استثنائي لجميع الرياضيين",
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
                    Description = "صوتيات احترافية وكاميرات وتقنيات ترفيهية رائدة عالمياً",
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
                    Description = "دار الأزياء والعطور الفرنسية الفاخرة ذات اللمسات الأسطورية",
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
                    Description = "حواسيب محمولة ومكتبية قوية وشاشات متميزة للمحترفين",
                    ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=300&auto=format&fit=crop&q=80",
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

            var electronicsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "electronics" || c.Slug == "smartphones");
            var fashionCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "clothing-fashion" || c.Slug == "clothing");
            var shoesCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "shoes-bags");
            var homeCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "home-kitchen" || c.Slug == "home-garden");
            var beautyCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "beauty-perfumes");
            var sportsCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "sports-fitness" || c.Slug == "sports-outdoors");
            var watchesCat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "watches-accessories");

            var appleBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "apple");
            var samsungBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "samsung");
            var nikeBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "nike");
            var adidasBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "adidas");
            var sonyBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "sony");
            var zaraBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "zara");
            var diorBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "dior");
            var dellBrand = await db.Brands.FirstOrDefaultAsync(b => b.Slug == "dell");

            var productsToSeed = new List<(
                Product product,
                List<(string url, bool isPrimary, string alt)> images,
                List<(string name, string sku, decimal price, decimal compareAt)> variants,
                int stock
            )>
            {
                // 1. iPhone 15 Pro Max
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = electronicsCat?.Id,
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=80", true, "آيفون 15 برو ماكس من الأمام والخلف"),
                        ("https://images.unsplash.com/photo-1695048065059-d2d8ceeb0f2c?w=800&auto=format&fit=crop&q=80", false, "آيفون 15 برو ماكس تيتانيوم طبيعي"),
                        ("https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=800&auto=format&fit=crop&q=80", false, "شاشة آيفون فائقة السطوع")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("تيتانيوم طبيعي 256 جيجابايت", "APL-IP15PM-256-NAT", 1199m, 1299m),
                        ("تيتانيوم أسود 512 جيجابايت", "APL-IP15PM-512-BLK", 1399m, 1499m),
                        ("تيتانيوم أزرق 1 تيرابايت", "APL-IP15PM-1TB-BLU", 1599m, 1699m)
                    },
                    45
                ),

                // 2. Sony WH-1000XM5
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = electronicsCat?.Id,
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80", true, "سماعات سوني WH-1000XM5"),
                        ("https://images.unsplash.com/photo-1484704849700-f032a568e944?w=800&auto=format&fit=crop&q=80", false, "تفاصيل سماعة الرأس")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("أسود كلاسيكي (Black)", "SNY-WH1000XM5-BLK", 349m, 399m),
                        ("فضي بلاتيني (Silver)", "SNY-WH1000XM5-SLV", 349m, 399m)
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800&auto=format&fit=crop&q=80", true, "حذاء نايكي إير ماكس أحمر"),
                        ("https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=800&auto=format&fit=crop&q=80", false, "تفاصيل حذاء نايكي")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("مقاس 41 - أحمر/أسود", "NKE-AIRMAX-41", 175m, 199m),
                        ("مقاس 42 - أحمر/أسود", "NKE-AIRMAX-42", 175m, 199m),
                        ("مقاس 43 - أحمر/أسود", "NKE-AIRMAX-43", 175m, 199m),
                        ("مقاس 44 - أحمر/أسود", "NKE-AIRMAX-44", 175m, 199m)
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", true, "ساعة يد ذكية"),
                        ("https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=800&auto=format&fit=crop&q=80", false, "ساعة أبل بالمعصم")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("هيكل ألمنيوم 41 مم - سماء الليل", "APL-W-S9-41-MID", 399m, 429m),
                        ("هيكل ألمنيوم 45 مم - ضوء النجوم", "APL-W-S9-45-STR", 429m, 459m)
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1541643600914-78b084683601?w=800&auto=format&fit=crop&q=80", true, "زجاجة عطر سوفاج ديور"),
                        ("https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&auto=format&fit=crop&q=80", false, "عطر فاخر أصلي")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("حجم 60 مل", "DIOR-SVG-60ML", 110m, 125m),
                        ("حجم 100 مل", "DIOR-SVG-100ML", 145m, 165m),
                        ("حجم 200 مل", "DIOR-SVG-200ML", 210m, 240m)
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1544441893-675973e31985?w=800&auto=format&fit=crop&q=80", true, "جاكيت شتوي أنيق"),
                        ("https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?w=800&auto=format&fit=crop&q=80", false, "ملابس زارا الشتوية")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("أسود - مقاس M", "ZRA-PUFF-BLK-M", 89m, 120m),
                        ("أسود - مقاس L", "ZRA-PUFF-BLK-L", 89m, 120m),
                        ("زيتي - مقاس M", "ZRA-PUFF-OLV-M", 89m, 120m),
                        ("زيتي - مقاس L", "ZRA-PUFF-OLV-L", 89m, 120m)
                    },
                    90
                ),

                // 7. Dell XPS 15 Laptop
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = electronicsCat?.Id,
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80", true, "لابتوب ديل XPS 15"),
                        ("https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&auto=format&fit=crop&q=80", false, "شاشة ديل فائقة النقاء")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("معالج i7 - رام 16GB - سعة 512GB SSD", "DLL-XPS15-16-512", 1499m, 1699m),
                        ("معالج i9 - رام 32GB - سعة 1TB SSD", "DLL-XPS15-32-1TB", 1899m, 2099m)
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=800&auto=format&fit=crop&q=80", true, "شاشة تلفزيون سامسونج ذكية"),
                        ("https://images.unsplash.com/photo-1461151304267-38535e780c79?w=800&auto=format&fit=crop&q=80", false, "غرفة جلوس مع تلفزيون سامسونج")
                    },
                    new List<(string, string, decimal, decimal)>(),
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=800&auto=format&fit=crop&q=80", true, "حذاء أديداس ألترا بوست أبيض"),
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", false, "تفاصيل نعل ألترا بوست")
                    },
                    new List<(string, string, decimal, decimal)>
                    {
                        ("أبيض ناصع - مقاس 42", "ADS-UB-WHT-42", 180m, 210m),
                        ("أبيض ناصع - مقاس 43", "ADS-UB-WHT-43", 180m, 210m),
                        ("أسود كور - مقاس 42", "ADS-UB-BLK-42", 180m, 210m),
                        ("أسود كور - مقاس 44", "ADS-UB-BLK-44", 180m, 210m)
                    },
                    65
                ),

                // 10. De'Longhi Dedica Espresso Machine
                (
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        CategoryId = homeCat?.Id,
                        BrandId = null,
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", true, "ماكينة قهوة إسبريسو"),
                        ("https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=800&auto=format&fit=crop&q=80", false, "فنجان قهوة محضر بالماكينة")
                    },
                    new List<(string, string, decimal, decimal)>(),
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&auto=format&fit=crop&q=80", true, "حقيبة ظهر ذكية سوداء"),
                        ("https://images.unsplash.com/photo-1622560480605-d83c853bc5c3?w=800&auto=format&fit=crop&q=80", false, "تفاصيل الجيوب والملحقات")
                    },
                    new List<(string, string, decimal, decimal)>(),
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
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    },
                    new List<(string, bool, string)>
                    {
                        ("https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2?w=800&auto=format&fit=crop&q=80", true, "دمبل تمارين رياضية"),
                        ("https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=800&auto=format&fit=crop&q=80", false, "تدريب لياقة وأثقال")
                    },
                    new List<(string, string, decimal, decimal)>(),
                    40
                )
            };

            foreach (var item in productsToSeed)
            {
                var existingProduct = await db.Products
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
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
                    existingProduct.IsFeatured = item.product.IsFeatured;
                    existingProduct.IsActive = item.product.IsActive;

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

                    if (!existingProduct.InventoryItems.Any())
                    {
                        var prodInv = new InventoryItem(existingProduct.Id, mainWarehouse.Id, item.stock);
                        await db.InventoryItems.AddAsync(prodInv);
                    }

                    await db.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Seeded and synchronized rich products and variants");
        }

        private async Task SeedSampleOrdersAsync(ApplicationDbContext db)
        {
            if (await db.Orders.AnyAsync()) return;

            var iphone = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "iphone-15-pro-max");
            var appleWatch = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "apple-watch-series-9");
            var sony = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "sony-wh-1000xm5-headphones");
            var dell = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "dell-xps-15-oled-laptop");
            var backpack = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "smart-waterproof-anti-theft-backpack");
            var nike = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "nike-air-max-plus-sneakers");
            var adidas = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "adidas-ultraboost-1-sneakers");
            var dumbbells = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "adjustable-smart-dumbbell-set-24kg");
            var zara = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "zara-winter-puffer-jacket");
            var dior = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Slug == "dior-sauvage-edp-perfume");

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
