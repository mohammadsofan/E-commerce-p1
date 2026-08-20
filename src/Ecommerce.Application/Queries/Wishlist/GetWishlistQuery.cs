using System.Collections.Generic;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Wishlist
{
    public class GetWishlistQuery : IQuery<List<WishlistItemDto>>
    {
    }
}
