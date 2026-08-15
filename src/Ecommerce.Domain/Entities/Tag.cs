using System;

namespace Ecommerce.Domain.Entities
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }
}
