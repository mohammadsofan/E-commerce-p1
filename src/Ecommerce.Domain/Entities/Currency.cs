using System;

namespace Ecommerce.Domain.Entities
{
    public class Currency
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Symbol { get; set; }
        public bool IsBaseCurrency { get; set; }
    }
}
