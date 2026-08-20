using System;

namespace Ecommerce.Application.DTOs
{
    public class WishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string? ProductImageUrl { get; set; }
        public int AvailableStock { get; set; }
        public bool IsActive { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
