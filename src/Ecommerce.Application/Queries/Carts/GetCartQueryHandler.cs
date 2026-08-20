using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common.Carts;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Application.Queries.Carts
{
    public class GetCartQueryHandler : CartAccessor, IQueryHandler<GetCartQuery, CartDto>
    {
        public GetCartQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMapper mapper)
            : base(db, currentUser, mapper)
        {
        }

        public async Task<CartDto> Handle(GetCartQuery query, CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartAsync(cancellationToken);
            return await MapAsync(cart, cancellationToken);
        }
    }
}
