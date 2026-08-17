using System;
using System.Collections.Generic;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetCurrenciesQuery : IQuery<List<CurrencyDto>>
    {
    }

    public class GetAdminCurrenciesQuery : IQuery<PagedResult<CurrencyDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAdminCurrencyByIdQuery : IQuery<CurrencyDto>
    {
        public Guid Id { get; set; }
    }

    public class GetExchangeRatesQuery : IQuery<List<ExchangeRateDto>>
    {
    }

    public class GetAdminExchangeRatesQuery : IQuery<PagedResult<ExchangeRateDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? FromCurrencyId { get; set; }
        public Guid? ToCurrencyId { get; set; }
    }

    public class GetAdminExchangeRateByIdQuery : IQuery<ExchangeRateDto>
    {
        public Guid Id { get; set; }
    }

    public class ConvertCurrencyQuery : IQuery<CurrencyConversionResult>
    {
        public decimal Amount { get; set; }
        public string From { get; set; } = "USD";
        public string To { get; set; } = "EUR";
    }
}