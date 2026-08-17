using System;

namespace Ecommerce.Domain.Entities
{
    public class Currency
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsBaseCurrency { get; set; }
    }
}
