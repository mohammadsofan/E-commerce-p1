using System;

namespace Ecommerce.Domain.Entities
{
    public class ExchangeRate
    {
        public Guid Id { get; set; }
        public Guid FromCurrencyId { get; set; }
        public Guid ToCurrencyId { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset EffectiveAt { get; set; }
    }
}
