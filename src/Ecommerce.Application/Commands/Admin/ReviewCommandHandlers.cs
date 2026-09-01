using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class SubmitProductReviewCommandHandler : ICommandHandler<SubmitProductReviewCommand, ProductReviewDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public SubmitProductReviewCommandHandler(IApplicationDbContext db, IMapper mapper, ICurrentUserService currentUser)
        {
            _db = db;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<ProductReviewDto> Handle(SubmitProductReviewCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Rating < 1 || command.Rating > 5)
                throw new DomainException("Rating must be between 1 and 5");

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
            if (product == null)
                throw new NotFoundException("Product", command.ProductId);

            var userId = _currentUser.UserId;
            var userName = _currentUser.UserName;
            var isAdmin = _currentUser.IsAdmin;

            var candidateUserIds = new List<Guid>();
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                candidateUserIds.Add(userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                var userDb = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == userName || u.Email == userName, cancellationToken);
                if (userDb != null && !candidateUserIds.Contains(userDb.Id))
                {
                    candidateUserIds.Add(userDb.Id);
                }
            }

            if (!candidateUserIds.Any() && !isAdmin)
                throw new DomainException("يجب تسجيل الدخول أولاً لكتابة تقييم.");

            var effectiveUserId = candidateUserIds.FirstOrDefault(Guid.NewGuid());

            // Collect product ID and all related variant IDs
            var variantIds = await _db.ProductVariants
                .AsNoTracking()
                .Where(v => v.ProductId == command.ProductId)
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);
            var allTargetIds = new List<Guid>(variantIds) { command.ProductId };

            // Verified Purchase Requirement: Customer must have an order with Status == OrderStatus.Completed containing the product, or be an Admin
            var hasCompletedOrder = isAdmin;
            if (!hasCompletedOrder && candidateUserIds.Any())
            {
                var candidateNullableIds = candidateUserIds.Select(id => (Guid?)id).ToList();

                var completedOrderIds = await _db.Orders
                    .AsNoTracking()
                    .Where(o => candidateNullableIds.Contains(o.UserId) && o.Status == OrderStatus.Completed)
                    .Select(o => o.Id)
                    .ToListAsync(cancellationToken);

                if (completedOrderIds.Any())
                {
                    hasCompletedOrder = await _db.OrderItems
                        .AsNoTracking()
                        .AnyAsync(oi => completedOrderIds.Contains(oi.OrderId) && (allTargetIds.Contains(oi.ProductId) || (oi.ProductVariantId.HasValue && allTargetIds.Contains(oi.ProductVariantId.Value))), cancellationToken);
                }
            }

            if (!hasCompletedOrder)
            {
                throw new DomainException("لا يمكنك تقييم هذا المنتج إلا بعد استلام طلبك واكتمال حالته (Completed).");
            }

            var now = DateTimeOffset.UtcNow;
            var productReviews = await _db.ProductReviews
                .Where(r => r.ProductId == command.ProductId)
                .ToListAsync(cancellationToken);

            var existingReview = candidateUserIds.Any()
                ? productReviews.FirstOrDefault(r => candidateUserIds.Contains(r.UserId))
                : null;

            var isApproved = isAdmin || string.IsNullOrWhiteSpace(command.Comment);

            ProductReview review;
            if (existingReview != null)
            {
                existingReview.Rating = command.Rating;
                existingReview.Title = command.Title ?? string.Empty;
                existingReview.Comment = command.Comment ?? string.Empty;
                existingReview.IsVerifiedPurchase = true;
                existingReview.IsApproved = isApproved;
                existingReview.UpdatedAt = now;
                review = existingReview;
            }
            else
            {
                review = new ProductReview
                {
                    Id = Guid.NewGuid(),
                    ProductId = command.ProductId,
                    UserId = effectiveUserId,
                    Rating = command.Rating,
                    Title = command.Title ?? string.Empty,
                    Comment = command.Comment ?? string.Empty,
                    IsVerifiedPurchase = true,
                    IsApproved = isApproved,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.ProductReviews.Add(review);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await ReviewStatsUpdater.UpdateProductRatingStatsAsync(_db, command.ProductId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<ProductReviewDto>(review);
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == review.UserId, cancellationToken);
            dto.UserDisplayName = user != null && !string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.DisplayName
                : user?.UserName ?? string.Empty;

            return dto;
        }
    }

    public class UpdateReviewStatusCommandHandler : ICommandHandler<UpdateReviewStatusCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public UpdateReviewStatusCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(UpdateReviewStatusCommand command, CancellationToken cancellationToken = default)
        {
            var review = await _db.ProductReviews
                .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
            if (review == null)
                throw new NotFoundException("Review", command.Id);

            review.IsApproved = command.IsApproved;
            review.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await ReviewStatsUpdater.UpdateProductRatingStatsAsync(_db, review.ProductId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class DeleteReviewCommandHandler : ICommandHandler<DeleteReviewCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteReviewCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteReviewCommand command, CancellationToken cancellationToken = default)
        {
            var review = await _db.ProductReviews
                .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
            if (review == null)
                throw new NotFoundException("Review", command.Id);

            var productId = review.ProductId;
            _db.ProductReviews.Remove(review);
            await _db.SaveChangesAsync(cancellationToken);
            await ReviewStatsUpdater.UpdateProductRatingStatsAsync(_db, productId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    internal static class ReviewStatsUpdater
    {
        public static async Task UpdateProductRatingStatsAsync(IApplicationDbContext db, Guid productId, CancellationToken cancellationToken)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product == null) return;

            var approvedReviews = await db.ProductReviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .ToListAsync(cancellationToken);

            product.ReviewCount = approvedReviews.Count;
            product.AverageRating = approvedReviews.Count > 0
                ? Math.Round((decimal)approvedReviews.Average(r => r.Rating), 2)
                : 0m;
        }
    }
}