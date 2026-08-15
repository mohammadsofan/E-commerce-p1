using System;

namespace Ecommerce.Domain.Exceptions
{
    public class InventoryException : DomainException
    {
        public InventoryException(string message) : base(message) { }
    }
}
