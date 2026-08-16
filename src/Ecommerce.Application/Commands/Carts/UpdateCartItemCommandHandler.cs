using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Commands.Carts
{
    public class UpdateCartItemCommandHandler : CartAccessor, ICommandHandler<UpdateCartItemCommand, CartDto>
    {
        public UpdateCartItemCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
        {
            // A quantity <= 0 removes the line (handled inside the aggregate).
            var cart = await GetCartOrThrowAsync(cancellationToken);
            cart.UpdateItemQuantity(command.CartItemId, command.Quantity);

            await Db.SaveChangesAsync(cancellationToken);
            return Map(cart);
        }
    }
}
