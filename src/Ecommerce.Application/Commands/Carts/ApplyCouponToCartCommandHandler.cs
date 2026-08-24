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

            var now = DateTimeOffset.UtcNow;
            if (coupon == null || !coupon.IsActive || (coupon.EndAt.HasValue && coupon.EndAt.Value < now) || (coupon.StartAt.HasValue && coupon.StartAt.Value > now))
                throw new DomainException("كود الخصم غير صحيح أو منتهي الصلاحية");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                throw new DomainException("تجاوز الكوبون حد الاستخدام المسموح به");

            if (coupon.MinOrderAmount.HasValue && cart.Subtotal < coupon.MinOrderAmount.Value)
                throw new DomainException("لم يتم الوصول للحد الأدنى للطلب لاستخدام هذا الكوبون");

            cart.ApplyCoupon(coupon.Code);
            await Db.SaveChangesAsync(cancellationToken);

            return await MapAsync(cart, cancellationToken);
        }
    }
}
