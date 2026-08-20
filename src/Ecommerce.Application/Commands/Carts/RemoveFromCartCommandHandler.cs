using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Commands.Carts
{
    public class RemoveFromCartCommandHandler : CartAccessor, ICommandHandler<RemoveFromCartCommand, CartDto>
    {
        public RemoveFromCartCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(RemoveFromCartCommand command, CancellationToken cancellationToken = default)
        {
            var cart = await GetCartOrThrowAsync(cancellationToken);
            cart.RemoveItem(command.CartItemId);

            await Db.SaveChangesAsync(cancellationToken);
            return await MapAsync(cart, cancellationToken);
        }
    }
}
