using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Carts
{
    public class ApplyCouponToCartCommandHandler : CartAccessor, ICommandHandler<ApplyCouponToCartCommand, CartDto>
    {
        public ApplyCouponToCartCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(ApplyCouponToCartCommand command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Code))
                throw new DomainException("Coupon code is required");

            var cart = await GetOrCreateCartAsync(cancellationToken);
            if (cart.Items.Count == 0 || cart.Subtotal <= 0)
                throw new DomainException("Cannot apply coupon to an empty cart");

            var upperCode = command.Code.Trim().ToUpperInvariant();
            var coupon = await Db.Coupons
                .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

            if (coupon == null || !coupon.IsActive)
                throw new DomainException("Invalid or inactive coupon code");

            var now = DateTimeOffset.UtcNow;
            if (coupon.StartAt.HasValue && coupon.StartAt.Value > now)
                throw new DomainException("Coupon is not yet active");

            if (coupon.EndAt.HasValue && coupon.EndAt.Value < now)
                throw new DomainException("Coupon has expired");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                throw new DomainException("Coupon usage limit has been reached");

            if (coupon.MinOrderAmount.HasValue && cart.Subtotal < coupon.MinOrderAmount.Value)
                throw new DomainException($"Minimum order subtotal of {coupon.MinOrderAmount.Value:F2} is required to use this coupon");

            // Calculate discount amount
            decimal discountAmount = 0m;
            var type = (coupon.Type ?? string.Empty).ToLowerInvariant();

            if (type == "percentage")
            {
                discountAmount = cart.Subtotal * (coupon.Value / 100m);
                if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0)
                {
                    discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
                }
            }
            else if (type == "fixed_amount")
            {
                discountAmount = coupon.Value;
            }
            else if (type == "free_shipping")
            {
                discountAmount = 0m;
            }
            else
            {
                discountAmount = coupon.Value;
            }

            discountAmount = Math.Max(0m, Math.Min(cart.Subtotal, discountAmount));

            cart.ApplyCoupon(coupon.Code, discountAmount);
            await Db.SaveChangesAsync(cancellationToken);

            return await MapAsync(cart, cancellationToken);
        }
    }
}
