using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Commands.Carts
{
    public class ClearCartCommandHandler : CartAccessor, ICommandHandler<ClearCartCommand, CartDto>
    {
        public ClearCartCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(ClearCartCommand command, CancellationToken cancellationToken = default)
        {
            // Idempotent: an empty (or missing) cart clears to an empty cart.
            var cart = await GetOrCreateCartAsync(cancellationToken);
            cart.Clear();

            await Db.SaveChangesAsync(cancellationToken);
            return Map(cart);
        }
    }
}
