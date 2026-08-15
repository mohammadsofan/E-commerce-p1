using System;

namespace Ecommerce.Domain.Entities
{
    public class TaxCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
