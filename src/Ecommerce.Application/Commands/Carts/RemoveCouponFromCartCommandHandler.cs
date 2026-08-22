using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Commands.Carts
{
    public class RemoveCouponFromCartCommandHandler : CartAccessor, ICommandHandler<RemoveCouponFromCartCommand, CartDto>
    {
        public RemoveCouponFromCartCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(RemoveCouponFromCartCommand command, CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(cancellationToken);
            cart.RemoveCoupon();
            await Db.SaveChangesAsync(cancellationToken);

            return await MapAsync(cart, cancellationToken);
        }
    }
}
