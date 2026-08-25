using System;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class ProductReviewHandlerTests
    {
        private class TestCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; set; }
            public string? UserName { get; set; }
            public bool IsAdmin { get; set; }
        }

        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static IMapper CreateTestMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task SubmitReview_ThrowsDomainException_WhenUserHasNotPurchasedProduct()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Slug = "test-product",
                BasePrice = 100m,
                IsActive = true
            };
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            var handler = new SubmitProductReviewCommandHandler(db, mapper, currentUser);
            var command = new SubmitProductReviewCommand
            {
                ProductId = product.Id,
                Rating = 5,
                Title = "Great product",
                Comment = "Loved it!"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
            Assert.Contains("Completed", ex.Message);
        }

        [Fact]
        public async Task SubmitReview_ThrowsDomainException_WhenOrderIsPendingOrProcessing()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId, UserName = "buyer@example.com" };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Phone",
                Slug = "test-phone",
                BasePrice = 500m,
                IsActive = true
            };
            await db.Products.AddAsync(product);

            // User has an order with Placed status (not yet Completed)
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = "ORD-999"
            };
            order.AddItem(product.Id, Guid.NewGuid(), product.Name, 500m, 1);
            order.PlaceOrder();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var handler = new SubmitProductReviewCommandHandler(db, mapper, currentUser);
            var command = new SubmitProductReviewCommand
            {
                ProductId = product.Id,
                Rating = 5,
                Title = "Super phone!",
                Comment = "Battery lasts all day."
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));
            Assert.Contains("Completed", ex.Message);
        }

        [Fact]
        public async Task SubmitReview_Succeeds_WhenUserHasCompletedOrder()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId, UserName = "buyer@example.com" };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Phone",
                Slug = "test-phone",
                BasePrice = 500m,
                IsActive = true
            };
            await db.Products.AddAsync(product);

            // User has a Completed order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = "ORD-999"
            };
            order.AddItem(product.Id, Guid.NewGuid(), product.Name, 500m, 1);
            order.PlaceOrder();
            order.MarkPaid();
            order.Complete();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var handler = new SubmitProductReviewCommandHandler(db, mapper, currentUser);
            var command = new SubmitProductReviewCommand
            {
                ProductId = product.Id,
                Rating = 5,
                Title = "Super phone!",
                Comment = "Battery lasts all day."
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Rating);
            Assert.Equal("Super phone!", result.Title);
            Assert.True(result.IsVerifiedPurchase);
            // Review with comment text requires moderation for non-admin
            Assert.False(result.IsApproved);
        }

        [Fact]
        public async Task SubmitReview_RatingOnly_AutoApproved_And_UpdatesProductRating()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId, UserName = "buyer@example.com" };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Phone",
                Slug = "test-phone",
                BasePrice = 500m,
                IsActive = true
            };
            await db.Products.AddAsync(product);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = "ORD-100"
            };
            order.AddItem(product.Id, Guid.NewGuid(), product.Name, 500m, 1);
            order.PlaceOrder();
            order.MarkPaid();
            order.Complete();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var handler = new SubmitProductReviewCommandHandler(db, mapper, currentUser);
            var command = new SubmitProductReviewCommand
            {
                ProductId = product.Id,
                Rating = 4,
                Title = "",
                Comment = ""
            };

            // Act
            var result = await handler.Handle(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Rating);
            Assert.True(result.IsApproved);

            var updatedProduct = await db.Products.FindAsync(product.Id);
            Assert.NotNull(updatedProduct);
            Assert.Equal(1, updatedProduct.ReviewCount);
            Assert.Equal(4.00m, updatedProduct.AverageRating);
        }

        [Fact]
        public async Task UpdateReviewStatus_RecalculatesProductRatingAndReviewCount()
        {
            // Arrange
            using var db = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Phone",
                Slug = "test-phone",
                BasePrice = 500m,
                IsActive = true,
                ReviewCount = 0,
                AverageRating = 0m
            };
            await db.Products.AddAsync(product);

            var review = new ProductReview
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                UserId = Guid.NewGuid(),
                Rating = 5,
                Comment = "Great!",
                IsApproved = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.ProductReviews.AddAsync(review);
            await db.SaveChangesAsync();

            var handler = new UpdateReviewStatusCommandHandler(db);
            var command = new UpdateReviewStatusCommand { Id = review.Id, IsApproved = true };

            // Act
            await handler.Handle(command);

            // Assert
            var updatedReview = await db.ProductReviews.FindAsync(review.Id);
            Assert.NotNull(updatedReview);
            Assert.True(updatedReview.IsApproved);

            var updatedProduct = await db.Products.FindAsync(product.Id);
            Assert.NotNull(updatedProduct);
            Assert.Equal(1, updatedProduct.ReviewCount);
            Assert.Equal(5.00m, updatedProduct.AverageRating);
        }

        [Fact]
        public async Task DeleteReview_RecalculatesProductRatingAndReviewCount()
        {
            // Arrange
            using var db = CreateInMemoryContext();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Phone",
                Slug = "test-phone",
                BasePrice = 500m,
                IsActive = true,
                ReviewCount = 2,
                AverageRating = 4.5m
            };
            await db.Products.AddAsync(product);

            var review1 = new ProductReview
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                UserId = Guid.NewGuid(),
                Rating = 5,
                IsApproved = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var review2 = new ProductReview
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                UserId = Guid.NewGuid(),
                Rating = 4,
                IsApproved = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await db.ProductReviews.AddRangeAsync(review1, review2);
            await db.SaveChangesAsync();

            var handler = new DeleteReviewCommandHandler(db);
            var command = new DeleteReviewCommand { Id = review2.Id };

            // Act
            await handler.Handle(command);

            // Assert
            var updatedProduct = await db.Products.FindAsync(product.Id);
            Assert.NotNull(updatedProduct);
            Assert.Equal(1, updatedProduct.ReviewCount);
            Assert.Equal(5.00m, updatedProduct.AverageRating);
        }

        [Fact]
        public async Task GetReviewEligibility_ReturnsCanReviewFalse_WhenOrderNotCompleted()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Headphones",
                Slug = "headphones",
                BasePrice = 50m,
                IsActive = true
            };
            await db.Products.AddAsync(product);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = "ORD-123"
            };
            order.AddItem(product.Id, Guid.NewGuid(), product.Name, 50m, 1);
            order.PlaceOrder();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var handler = new GetProductReviewEligibilityQueryHandler(db, mapper, currentUser);
            var query = new GetProductReviewEligibilityQuery { ProductId = product.Id };

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.False(result.CanReview);
            Assert.False(result.HasPurchased);
        }

        [Fact]
        public async Task GetReviewEligibility_ReturnsCanReviewTrue_WhenOrderCompleted()
        {
            // Arrange
            using var db = CreateInMemoryContext();
            var mapper = CreateTestMapper();

            var userId = Guid.NewGuid();
            var currentUser = new TestCurrentUserService { UserId = userId };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Headphones",
                Slug = "headphones",
                BasePrice = 50m,
                IsActive = true
            };
            await db.Products.AddAsync(product);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = "ORD-123"
            };
            order.AddItem(product.Id, Guid.NewGuid(), product.Name, 50m, 1);
            order.PlaceOrder();
            order.MarkPaid();
            order.Complete();
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var handler = new GetProductReviewEligibilityQueryHandler(db, mapper, currentUser);
            var query = new GetProductReviewEligibilityQuery { ProductId = product.Id };

            // Act
            var result = await handler.Handle(query);

            // Assert
            Assert.True(result.CanReview);
            Assert.True(result.HasPurchased);
            Assert.False(result.HasReviewed);
        }
    }
}

