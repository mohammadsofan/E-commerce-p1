using System;

namespace Ecommerce.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public decimal BasePrice { get; set; }
    }
}
