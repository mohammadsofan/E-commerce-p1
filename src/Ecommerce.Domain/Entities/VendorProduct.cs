using System;

namespace Ecommerce.Domain.Entities
{
    public class VendorProduct
    {
        public Guid Id { get; set; }
        public Guid VendorId { get; set; }
        public Guid ProductId { get; set; }
        public string VendorSku { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
