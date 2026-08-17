using System;
using System.Threading;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCouponCommandHandler : ICommandHandler<CreateCouponCommand, AdminCouponDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateCouponCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminCouponDto> Handle(CreateCouponCommand command, CancellationToken cancellationToken = default)
        {
            var upperCode = command.Code.ToUpperInvariant();
            var existingCoupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

            if (existingCoupon != null)
                throw new Domain.Exceptions.DomainException("Coupon code already exists");

            var coupon = new Coupon
            {
                Code = command.Code.ToUpperInvariant(),
                Description = command.Description,
                Type = command.Type,
                Value = command.Value,
                StartAt = command.StartAt,
                EndAt = command.EndAt,
                MinOrderAmount = command.MinOrderAmount,
                MaxDiscountAmount = command.MaxDiscountAmount,
                UsageLimit = command.UsageLimit,
                PerUserLimit = command.PerUserLimit,
                IsActive = command.IsActive,
                AllowCombine = command.AllowCombine,
                ApplicableProductIds = command.ApplicableProductIds,
                ApplicableCategoryIds = command.ApplicableCategoryIds,
                ApplicableUserIds = command.ApplicableUserIds,
                ExcludedProductIds = command.ExcludedProductIds,
                ExcludedCategoryIds = command.ExcludedCategoryIds,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminCouponDto>(coupon);
        }
    }

    public class UpdateCouponCommandHandler : ICommandHandler<UpdateCouponCommand, AdminCouponDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateCouponCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<AdminCouponDto> Handle(UpdateCouponCommand command, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons.FindAsync(new object[] { command.Id }, cancellationToken);

            if (coupon == null)
                throw new Domain.Exceptions.NotFoundException("Coupon", command.Id);

            // Optimistic concurrency check
            if (command.RowVersion.Length > 0)
            {
                var entry = _db.GetEntry(coupon);
                entry.OriginalValues["RowVersion"] = command.RowVersion;
            }

            // Check for code uniqueness if changed
            var upperCode = command.Code.ToUpperInvariant();
            if (!string.Equals(coupon.Code, upperCode, StringComparison.OrdinalIgnoreCase))
            {
                var existingCoupon = await _db.Coupons
                    .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

                if (existingCoupon != null)
                    throw new Domain.Exceptions.DomainException("Coupon code already exists");
            }

            coupon.Code = command.Code.ToUpperInvariant();
            coupon.Description = command.Description;
            coupon.Type = command.Type;
            coupon.Value = command.Value;
            coupon.StartAt = command.StartAt;
            coupon.EndAt = command.EndAt;
            coupon.MinOrderAmount = command.MinOrderAmount;
            coupon.MaxDiscountAmount = command.MaxDiscountAmount;
            coupon.UsageLimit = command.UsageLimit;
            coupon.PerUserLimit = command.PerUserLimit;
            coupon.IsActive = command.IsActive;
            coupon.AllowCombine = command.AllowCombine;
            coupon.ApplicableProductIds = command.ApplicableProductIds;
            coupon.ApplicableCategoryIds = command.ApplicableCategoryIds;
            coupon.ApplicableUserIds = command.ApplicableUserIds;
            coupon.ExcludedProductIds = command.ExcludedProductIds;
            coupon.ExcludedCategoryIds = command.ExcludedCategoryIds;
            coupon.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AdminCouponDto>(coupon);
        }
    }

    public class DeleteCouponCommandHandler : ICommandHandler<DeleteCouponCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteCouponCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteCouponCommand command, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

            if (coupon == null)
                throw new Domain.Exceptions.NotFoundException("Coupon", command.Id);

            if (coupon.Usages.Any())
                throw new Domain.Exceptions.DomainException("Cannot delete coupon that has been used");

            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}