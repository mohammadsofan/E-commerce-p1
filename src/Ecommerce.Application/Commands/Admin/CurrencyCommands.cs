using System;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCurrencyCommand : ICommand<CurrencyDto>
    {
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsBaseCurrency { get; set; }
    }

    public class UpdateCurrencyCommand : ICommand<CurrencyDto>
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public bool IsBaseCurrency { get; set; }
    }

    public class DeleteCurrencyCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }

    public class CreateExchangeRateCommand : ICommand<ExchangeRateDto>
    {
        public Guid FromCurrencyId { get; set; }
        public Guid ToCurrencyId { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset? EffectiveAt { get; set; }
    }

    public class UpdateExchangeRateCommand : ICommand<ExchangeRateDto>
    {
        public Guid Id { get; set; }
        public Guid FromCurrencyId { get; set; }
        public Guid ToCurrencyId { get; set; }
        public decimal Rate { get; set; }
        public DateTimeOffset? EffectiveAt { get; set; }
    }

    public class DeleteExchangeRateCommand : ICommand<Unit>
    {
        public Guid Id { get; set; }
    }
}