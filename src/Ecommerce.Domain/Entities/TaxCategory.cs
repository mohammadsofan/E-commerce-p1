using System;
using System.Collections.Generic;

namespace Ecommerce.Domain.Entities
{
    public class TaxCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<TaxRate> Rates { get; set; } = new List<TaxRate>();
    }
}
