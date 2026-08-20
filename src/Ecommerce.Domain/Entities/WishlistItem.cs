using System;

namespace Ecommerce.Domain.Entities
{
    public class WishlistItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Product? Product { get; set; }

        public static WishlistItem Create(Guid userId, Guid productId)
        {
            return new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
