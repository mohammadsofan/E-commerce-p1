using System;
using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public List<AdminProductImageDto> Images { get; set; } = new List<AdminProductImageDto>();
    }
}