using System;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.StoreFeatures;
using Ecommerce.Application.Queries.StoreFeatures;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class StoreFeatureHandlerTests
    {
        private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateStoreFeature_CreatesAndReturnsDto()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var handler = new CreateStoreFeatureCommandHandler(db);
            var command = new CreateStoreFeatureCommand
            {
                Title = "????? ?????",
                Description = "??????? ??? 50 ?????",
                IconName = "Truck",
                DisplayOrder = 1,
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("????? ?????", result.Title);
            Assert.Equal("??????? ??? 50 ?????", result.Description);
            Assert.Equal("Truck", result.IconName);
            Assert.Equal(1, result.DisplayOrder);
            Assert.True(result.IsActive);

            var inDb = await db.StoreFeatures.FindAsync(result.Id);
            Assert.NotNull(inDb);
            Assert.Equal("????? ?????", inDb.Title);
        }

        [Fact]
        public async Task UpdateStoreFeature_UpdatesPropertiesCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var feature = new StoreFeature
            {
                Id = Guid.NewGuid(),
                Title = "Old Title",
                Description = "Old Desc",
                IconName = "Shield",
                DisplayOrder = 2,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            db.StoreFeatures.Add(feature);
            await db.SaveChangesAsync();

            var handler = new UpdateStoreFeatureCommandHandler(db);
            var command = new UpdateStoreFeatureCommand
            {
                Id = feature.Id,
                Title = "New Title",
                Description = "New Desc",
                IconName = "RotateCcw",
                DisplayOrder = 5,
                IsActive = true
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.Equal("New Title", result.Title);
            Assert.Equal("New Desc", result.Description);
            Assert.Equal("RotateCcw", result.IconName);
            Assert.Equal(5, result.DisplayOrder);
            Assert.True(result.IsActive);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task UpdateStoreFeature_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var handler = new UpdateStoreFeatureCommandHandler(db);
            var command = new UpdateStoreFeatureCommand
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Description = "Desc"
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command));
        }

        [Fact]
        public async Task DeleteStoreFeature_DeletesFromDatabase()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var feature = new StoreFeature
            {
                Id = Guid.NewGuid(),
                Title = "To Delete",
                Description = "Desc",
                IconName = "Trash",
                CreatedAt = DateTime.UtcNow
            };
            db.StoreFeatures.Add(feature);
            await db.SaveChangesAsync();

            var handler = new DeleteStoreFeatureCommandHandler(db);
            var command = new DeleteStoreFeatureCommand { Id = feature.Id };

            // Act
            await handler.Handle(command);

            // Assert
            var inDb = await db.StoreFeatures.FindAsync(feature.Id);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task GetActiveFeatures_ReturnsOnlyActiveOrderedByDisplayOrder()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Feature 3", Description = "Desc", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow });
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Feature 1", Description = "Desc", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow });
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Inactive", Description = "Desc", DisplayOrder = 0, IsActive = false, CreatedAt = DateTime.UtcNow });
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Feature 2", Description = "Desc", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var handler = new GetActiveFeaturesQueryHandler(db);
            var query = new GetActiveFeaturesQuery();

            // Act
            var results = await handler.Handle(query);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal("Feature 1", results[0].Title);
            Assert.Equal("Feature 2", results[1].Title);
            Assert.Equal("Feature 3", results[2].Title);
        }

        [Fact]
        public async Task GetAdminFeatures_FiltersSearchAndActiveStatus()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Free Shipping", Description = "Fast delivery", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow });
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Secure Payment", Description = "Safe checkout", DisplayOrder = 2, IsActive = false, CreatedAt = DateTime.UtcNow });
            db.StoreFeatures.Add(new StoreFeature { Id = Guid.NewGuid(), Title = "Easy Returns", Description = "Free return shipping", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var handler = new GetAdminFeaturesQueryHandler(db);

            // Act: Search for "shipping"
            var searchResults = await handler.Handle(new GetAdminFeaturesQuery { Search = "shipping" });
            Assert.Equal(2, searchResults.TotalCount);

            // Act: Filter active only
            var activeResults = await handler.Handle(new GetAdminFeaturesQuery { IsActive = true });
            Assert.Equal(2, activeResults.TotalCount);
        }

        [Fact]
        public async Task GetFeatureById_WhenExists_ReturnsDto()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var feature = new StoreFeature
            {
                Id = Guid.NewGuid(),
                Title = "Quality Assurance",
                Description = "High grade",
                IconName = "Award",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.StoreFeatures.Add(feature);
            await db.SaveChangesAsync();

            var handler = new GetFeatureByIdQueryHandler(db);

            // Act
            var result = await handler.Handle(new GetFeatureByIdQuery { Id = feature.Id });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(feature.Id, result.Id);
            Assert.Equal("Quality Assurance", result.Title);
        }
    }
}

