using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
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
            if (!userId.HasValue)
                throw new DomainException("User is not authenticated");

            var now = DateTimeOffset.UtcNow;
            var review = new ProductReview
            {
                Id = Guid.NewGuid(),
                ProductId = command.ProductId,
                UserId = userId.Value,
                Rating = command.Rating,
                Title = command.Title,
                Comment = command.Comment,
                IsVerifiedPurchase = false,
                IsApproved = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.ProductReviews.Add(review);
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
                throw new DomainException("Review not found");

            review.IsApproved = command.IsApproved;
            review.UpdatedAt = DateTimeOffset.UtcNow;

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
                throw new DomainException("Review not found");

            _db.ProductReviews.Remove(review);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}