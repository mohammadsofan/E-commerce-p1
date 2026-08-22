using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.HeroBanners;
using Ecommerce.Application.Queries.HeroBanners;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class HeroBannerHandlerTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateHeroBanner_CreatesAndReturnsDto()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var handler = new CreateHeroBannerCommandHandler(db);
            var command = new CreateHeroBannerCommand
            {
                BadgeText = "مجموعة جديدة 2024",
                Title = "اكتشف منتجات مذهلة",
                Subtitle = "تسوق أفضل العروض",
                PrimaryButtonText = "تسوق الآن",
                PrimaryButtonLink = "/products",
                SecondaryButtonText = "تصفح التصنيفات",
                SecondaryButtonLink = "/categories",
                ImageUrl = "https://example.com/banner.png",
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("مجموعة جديدة 2024", result.BadgeText);
            Assert.Equal("اكتشف منتجات مذهلة", result.Title);
            Assert.Equal("تسوق أفضل العروض", result.Subtitle);
            Assert.Equal("تسوق الآن", result.PrimaryButtonText);
            Assert.Equal("/products", result.PrimaryButtonLink);
            Assert.Equal("تصفح التصنيفات", result.SecondaryButtonText);
            Assert.Equal("/categories", result.SecondaryButtonLink);
            Assert.Equal("https://example.com/banner.png", result.ImageUrl);
            Assert.True(result.IsActive);

            var inDb = await db.HeroBanners.FindAsync(result.Id);
            Assert.NotNull(inDb);
            Assert.Equal("اكتشف منتجات مذهلة", inDb.Title);
        }

        [Fact]
        public async Task UpdateHeroBanner_UpdatesPropertiesCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var banner = new HeroBanner
            {
                Id = Guid.NewGuid(),
                BadgeText = "Old Badge",
                Title = "Old Title",
                Subtitle = "Old Subtitle",
                PrimaryButtonText = "Old Btn",
                PrimaryButtonLink = "/old",
                SecondaryButtonText = "Old Sec",
                SecondaryButtonLink = "/old-sec",
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.HeroBanners.Add(banner);
            await db.SaveChangesAsync();

            var handler = new UpdateHeroBannerCommandHandler(db);
            var command = new UpdateHeroBannerCommand
            {
                Id = banner.Id,
                BadgeText = "New Badge",
                Title = "New Title",
                Subtitle = "New Subtitle",
                PrimaryButtonText = "New Btn",
                PrimaryButtonLink = "/new",
                SecondaryButtonText = "New Sec",
                SecondaryButtonLink = "/new-sec",
                ImageUrl = "https://example.com/new.png",
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.Equal("New Badge", result.BadgeText);
            Assert.Equal("New Title", result.Title);
            Assert.Equal("New Subtitle", result.Subtitle);
            Assert.Equal("New Btn", result.PrimaryButtonText);
            Assert.Equal("/new", result.PrimaryButtonLink);
            Assert.Equal("https://example.com/new.png", result.ImageUrl);
            Assert.True(result.IsActive);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task UpdateHeroBanner_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var handler = new UpdateHeroBannerCommandHandler(db);
            var command = new UpdateHeroBannerCommand
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Subtitle = "Subtitle"
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task SetActiveHeroBanner_TogglesTargetActiveStatus()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var banner1 = new HeroBanner { Id = Guid.NewGuid(), Title = "Banner 1", IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
            var banner2 = new HeroBanner { Id = Guid.NewGuid(), Title = "Banner 2", IsActive = false, CreatedAt = DateTimeOffset.UtcNow };
            db.HeroBanners.AddRange(banner1, banner2);
            await db.SaveChangesAsync();

            var handler = new SetActiveHeroBannerCommandHandler(db);
            var command = new SetActiveHeroBannerCommand { Id = banner2.Id };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.True(result.IsActive);
            var b1 = await db.HeroBanners.FindAsync(banner1.Id);
            var b2 = await db.HeroBanners.FindAsync(banner2.Id);
            Assert.True(b1!.IsActive);
            Assert.True(b2!.IsActive);
        }

        [Fact]
        public async Task DeleteHeroBanner_DeletesFromDatabase()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var banner = new HeroBanner
            {
                Id = Guid.NewGuid(),
                Title = "To Delete",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.HeroBanners.Add(banner);
            await db.SaveChangesAsync();

            var handler = new DeleteHeroBannerCommandHandler(db);
            var command = new DeleteHeroBannerCommand { Id = banner.Id };

            // Act
            await handler.Handle(command);

            // Assert
            var inDb = await db.HeroBanners.FindAsync(banner.Id);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task ReorderHeroBanners_UpdatesDisplayOrderInSequence()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var b1 = new HeroBanner { Id = Guid.NewGuid(), Title = "Banner 1", DisplayOrder = 1, CreatedAt = DateTimeOffset.UtcNow };
            var b2 = new HeroBanner { Id = Guid.NewGuid(), Title = "Banner 2", DisplayOrder = 2, CreatedAt = DateTimeOffset.UtcNow };
            var b3 = new HeroBanner { Id = Guid.NewGuid(), Title = "Banner 3", DisplayOrder = 3, CreatedAt = DateTimeOffset.UtcNow };
            db.HeroBanners.AddRange(b1, b2, b3);
            await db.SaveChangesAsync();

            var handler = new ReorderHeroBannersCommandHandler(db);
            // Reorder so that b3 is first, b1 is second, b2 is third
            var command = new ReorderHeroBannersCommand
            {
                BannerIds = new() { b3.Id, b1.Id, b2.Id }
            };

            // Act
            await handler.Handle(command);

            // Assert
            var updatedB1 = await db.HeroBanners.FindAsync(b1.Id);
            var updatedB2 = await db.HeroBanners.FindAsync(b2.Id);
            var updatedB3 = await db.HeroBanners.FindAsync(b3.Id);

            Assert.Equal(2, updatedB1!.DisplayOrder);
            Assert.Equal(3, updatedB2!.DisplayOrder);
            Assert.Equal(1, updatedB3!.DisplayOrder);
        }

        [Fact]
        public async Task GetActiveHeroBanners_ReturnsAllActiveBannersInDisplayOrder()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var b1 = new HeroBanner { Id = Guid.NewGuid(), Title = "Active Order 2", DisplayOrder = 2, IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
            var b2 = new HeroBanner { Id = Guid.NewGuid(), Title = "Active Order 1", DisplayOrder = 1, IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
            var b3 = new HeroBanner { Id = Guid.NewGuid(), Title = "Inactive", DisplayOrder = 0, IsActive = false, CreatedAt = DateTimeOffset.UtcNow };
            db.HeroBanners.AddRange(b1, b2, b3);
            await db.SaveChangesAsync();

            var handler = new GetActiveHeroBannersQueryHandler(db);
            var query = new GetActiveHeroBannersQuery();

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Active Order 1", result[0].Title);
            Assert.Equal("Active Order 2", result[1].Title);
        }

        [Fact]
        public async Task GetActiveHeroBanner_ReturnsFirstActiveBannerByDisplayOrder()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            db.HeroBanners.Add(new HeroBanner { Id = Guid.NewGuid(), Title = "Second Banner", DisplayOrder = 2, IsActive = true, CreatedAt = DateTimeOffset.UtcNow });
            db.HeroBanners.Add(new HeroBanner { Id = Guid.NewGuid(), Title = "First Banner", DisplayOrder = 1, IsActive = true, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();

            var handler = new GetActiveHeroBannerQueryHandler(db);
            var query = new GetActiveHeroBannerQuery();

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("First Banner", result.Title);
            Assert.Equal(1, result.DisplayOrder);
        }

        [Fact]
        public async Task GetAdminHeroBanners_FiltersSearchAndActiveStatus()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            db.HeroBanners.Add(new HeroBanner { Id = Guid.NewGuid(), Title = "Summer Collection", Subtitle = "Best dresses", IsActive = true, CreatedAt = DateTimeOffset.UtcNow });
            db.HeroBanners.Add(new HeroBanner { Id = Guid.NewGuid(), Title = "Winter Collection", Subtitle = "Warm jackets", IsActive = false, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();

            var handler = new GetAdminHeroBannersQueryHandler(db);

            // Act: Search
            var searchResults = await handler.Handle(new GetAdminHeroBannersQuery { Search = "summer" });
            Assert.Equal(1, searchResults.TotalCount);

            // Act: Filter active only
            var activeResults = await handler.Handle(new GetAdminHeroBannersQuery { IsActive = true });
            Assert.Equal(1, activeResults.TotalCount);
        }

        [Fact]
        public async Task GetHeroBannerById_WhenExists_ReturnsDto()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var banner = new HeroBanner
            {
                Id = Guid.NewGuid(),
                Title = "Banner Detail",
                DisplayOrder = 5,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.HeroBanners.Add(banner);
            await db.SaveChangesAsync();

            var handler = new GetHeroBannerByIdQueryHandler(db);

            // Act
            var result = await handler.Handle(new GetHeroBannerByIdQuery { Id = banner.Id });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(banner.Id, result.Id);
            Assert.Equal("Banner Detail", result.Title);
            Assert.Equal(5, result.DisplayOrder);
        }
    }
}

