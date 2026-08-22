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
                throw new DomainException("يرجى إدخال كود الخصم");

            var cart = await GetOrCreateCartAsync(cancellationToken);
            if (cart.Items.Count == 0 || cart.Subtotal <= 0)
                throw new DomainException("لا يمكن تطبيق كوبون على سلة تسوق فارغة");

            var upperCode = command.Code.Trim().ToUpperInvariant();
            var coupon = await Db.Coupons
                .FirstOrDefaultAsync(c => c.Code == upperCode, cancellationToken);

            if (coupon == null)
                throw new DomainException("كود الخصم غير صحيح");

            if (!coupon.IsActive)
                throw new DomainException("هذا الكوبون غير فعال");

            var now = DateTimeOffset.UtcNow;
            if (coupon.StartAt.HasValue && coupon.StartAt.Value > now)
                throw new DomainException("هذا الكوبون لم يبدأ تفعيله بعد");

            if (coupon.EndAt.HasValue && coupon.EndAt.Value < now)
                throw new DomainException("انتهت صلاحية الكوبون");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                throw new DomainException("تجاوز الكوبون حد الاستخدام المسموح به");

            if (coupon.MinOrderAmount.HasValue && cart.Subtotal < coupon.MinOrderAmount.Value)
                throw new DomainException("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون");

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
