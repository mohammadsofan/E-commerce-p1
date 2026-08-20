using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Wishlist
{
    public class AddToWishlistCommand : ICommand<WishlistItemDto>
    {
        public Guid ProductId { get; set; }
    }

    public class RemoveFromWishlistCommand : ICommand<Unit>
    {
        public Guid ProductId { get; set; }
    }

    public class ClearWishlistCommand : ICommand<Unit>
    {
    }
}
