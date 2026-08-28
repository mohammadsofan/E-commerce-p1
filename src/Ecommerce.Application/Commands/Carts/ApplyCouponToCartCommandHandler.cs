using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Discounts;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Carts
{
    public class ApplyCouponToCartCommandHandler : CartAccessor, ICommandHandler<ApplyCouponToCartCommand, CartDto>
    {
        public ApplyCouponToCartCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IMapper mapper,
            IPromotionEvaluationService? promotionEvaluator = null)
            : base(db, currentUser, mapper, promotionEvaluator)
        {
        }

        public async Task<CartDto> Handle(ApplyCouponToCartCommand command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Code))
                throw new DomainException("يرجى إدخال كود الخصم");

            var cart = await GetOrCreateCartAsync(cancellationToken);
            if (cart.Items.Count == 0 || cart.Subtotal <= 0)
                throw new DomainException("لا يمكن تطبيق كوبون على سلة تسوق فارغة");

            var upperCode = command.Code.Trim().ToUpperInvariant();
            var coupon = await Db.Coupons
                .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (coupon == null || !coupon.IsActive || (coupon.EndAt.HasValue && coupon.EndAt.Value < now) || (coupon.StartAt.HasValue && coupon.StartAt.Value > now))
                throw new DomainException("كود الخصم غير صحيح أو منتهي الصلاحية");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                throw new DomainException("تجاوز الكوبون حد الاستخدام المسموح به");

            if (coupon.PerUserLimit.HasValue && CurrentUser.UserId.HasValue && CurrentUser.UserId.Value != Guid.Empty)
            {
                var userUsageCount = await Db.CouponUsages
                    .CountAsync(u => u.CouponId == coupon.Id && u.UserId == CurrentUser.UserId.Value, cancellationToken);
                if (userUsageCount >= coupon.PerUserLimit.Value)
                    throw new DomainException("تجاوزت الحد الأقصى المسموح به لاستخدام هذا الكوبون");
            }

            cart.ApplyCoupon(coupon.Code);
            var cartDto = await MapAsync(cart, cancellationToken);

            var applicableSubtotal = Math.Max(0m, cartDto.Subtotal - cartDto.CartLevelDiscountAmount);
            if (coupon.MinOrderAmount.HasValue && applicableSubtotal < coupon.MinOrderAmount.Value)
            {
                cart.RemoveCoupon();
                throw new DomainException("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون");
            }

            // D-02: product/category scoping is enforced here with the same evaluator checkout
            // uses. A coupon whose scope excludes everything in the cart is rejected outright
            // rather than being stored and silently yielding a zero discount later.
            var couponLines = await BuildCouponLinesAsync(cartDto, cancellationToken);
            var evaluation = CouponDiscountCalculator.Calculate(coupon, couponLines, cartDto.CartLevelDiscountAmount);
            if (!evaluation.IsApplicable)
            {
                cart.RemoveCoupon();
                throw new DomainException(evaluation.RejectionReason ?? CouponDiscountCalculator.IneligibleProductsMessage);
            }

            await Db.SaveChangesAsync(cancellationToken);

            return cartDto;
        }
    }
}
