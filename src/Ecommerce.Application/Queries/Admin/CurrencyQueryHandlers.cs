using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Queries.Admin
{
    public class GetCurrenciesQueryHandler : IQueryHandler<GetCurrenciesQuery, List<CurrencyDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetCurrenciesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<CurrencyDto>> Handle(GetCurrenciesQuery query, CancellationToken cancellationToken = default)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .OrderByDescending(c => c.IsBaseCurrency)
                .ThenBy(c => c.Code)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<CurrencyDto>>(currencies);
        }
    }

    public class GetAdminCurrenciesQueryHandler : IQueryHandler<GetAdminCurrenciesQuery, PagedResult<CurrencyDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminCurrenciesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<CurrencyDto>> Handle(GetAdminCurrenciesQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.Currencies.AsNoTracking().AsQueryable();
            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(c => c.IsBaseCurrency)
                .ThenBy(c => c.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<CurrencyDto>
            {
                Items = _mapper.Map<List<CurrencyDto>>(items),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminCurrencyByIdQueryHandler : IQueryHandler<GetAdminCurrencyByIdQuery, CurrencyDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminCurrencyByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CurrencyDto> Handle(GetAdminCurrencyByIdQuery query, CancellationToken cancellationToken = default)
        {
            var currency = await _db.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);
            if (currency == null)
                throw new DomainException("Currency not found");

            return _mapper.Map<CurrencyDto>(currency);
        }
    }

    public class GetExchangeRatesQueryHandler : IQueryHandler<GetExchangeRatesQuery, List<ExchangeRateDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetExchangeRatesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<ExchangeRateDto>> Handle(GetExchangeRatesQuery query, CancellationToken cancellationToken = default)
        {
            var rates = await _db.ExchangeRates
                .AsNoTracking()
                .OrderByDescending(r => r.EffectiveAt)
                .ToListAsync(cancellationToken);

            return await ExchangeRateMapper.MapRatesWithCodesAsync(_db, _mapper, rates, cancellationToken);
        }
    }

    public class GetAdminExchangeRatesQueryHandler : IQueryHandler<GetAdminExchangeRatesQuery, PagedResult<ExchangeRateDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminExchangeRatesQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<PagedResult<ExchangeRateDto>> Handle(GetAdminExchangeRatesQuery query, CancellationToken cancellationToken = default)
        {
            var page = System.Math.Max(1, query.Page);
            var pageSize = System.Math.Clamp(query.PageSize, 1, 100);

            var q = _db.ExchangeRates.AsNoTracking().AsQueryable();
            if (query.FromCurrencyId.HasValue)
                q = q.Where(r => r.FromCurrencyId == query.FromCurrencyId.Value);
            if (query.ToCurrencyId.HasValue)
                q = q.Where(r => r.ToCurrencyId == query.ToCurrencyId.Value);

            var total = await q.CountAsync(cancellationToken);
            var items = await q
                .OrderByDescending(r => r.EffectiveAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ExchangeRateDto>
            {
                Items = await ExchangeRateMapper.MapRatesWithCodesAsync(_db, _mapper, items, cancellationToken),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public class GetAdminExchangeRateByIdQueryHandler : IQueryHandler<GetAdminExchangeRateByIdQuery, ExchangeRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public GetAdminExchangeRateByIdQueryHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ExchangeRateDto> Handle(GetAdminExchangeRateByIdQuery query, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ExchangeRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);
            if (rate == null)
                throw new DomainException("Exchange rate not found");

            var dto = await ExchangeRateMapper.MapRatesWithCodesAsync(_db, _mapper, new List<Domain.Entities.ExchangeRate> { rate }, cancellationToken);
            return dto.First();
        }
    }

    public class ConvertCurrencyQueryHandler : IQueryHandler<ConvertCurrencyQuery, CurrencyConversionResult>
    {
        private readonly IApplicationDbContext _db;

        public ConvertCurrencyQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CurrencyConversionResult> Handle(ConvertCurrencyQuery query, CancellationToken cancellationToken = default)
        {
            var from = query.From.ToUpperInvariant();
            var to = query.To.ToUpperInvariant();

            if (from == to)
            {
                return new CurrencyConversionResult
                {
                    Amount = query.Amount,
                    From = from,
                    To = to,
                    Rate = 1m,
                    ConvertedAmount = query.Amount,
                    AsOf = DateTimeOffset.UtcNow
                };
            }

            var currencies = await _db.Currencies.AsNoTracking().ToListAsync(cancellationToken);
            var fromCurrency = currencies.FirstOrDefault(c => c.Code == from);
            var toCurrency = currencies.FirstOrDefault(c => c.Code == to);

            if (fromCurrency == null)
                throw new DomainException($"Unknown currency: {from}");
            if (toCurrency == null)
                throw new DomainException($"Unknown currency: {to}");

            var now = DateTimeOffset.UtcNow;

            // Prefer the most recent rate effective at or before now.
            var rate = await _db.ExchangeRates.AsNoTracking()
                .Where(r => r.FromCurrencyId == fromCurrency.Id && r.ToCurrencyId == toCurrency.Id && r.EffectiveAt <= now)
                .OrderByDescending(r => r.EffectiveAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Fallback: invert the reverse rate.
            if (rate == null)
            {
                var reverse = await _db.ExchangeRates.AsNoTracking()
                    .Where(r => r.FromCurrencyId == toCurrency.Id && r.ToCurrencyId == fromCurrency.Id && r.EffectiveAt <= now)
                    .OrderByDescending(r => r.EffectiveAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (reverse != null && reverse.Rate != 0)
                {
                    var inverseRate = 1m / reverse.Rate;
                    return new CurrencyConversionResult
                    {
                        Amount = query.Amount,
                        From = from,
                        To = to,
                        Rate = inverseRate,
                        ConvertedAmount = query.Amount * inverseRate,
                        AsOf = reverse.EffectiveAt
                    };
                }

                throw new DomainException($"No exchange rate available from {from} to {to}");
            }

            return new CurrencyConversionResult
            {
                Amount = query.Amount,
                From = from,
                To = to,
                Rate = rate.Rate,
                ConvertedAmount = query.Amount * rate.Rate,
                AsOf = rate.EffectiveAt
            };
        }
    }

    internal static class ExchangeRateMapper
    {
        public static async Task<List<ExchangeRateDto>> MapRatesWithCodesAsync(
            IApplicationDbContext db, IMapper mapper, List<Domain.Entities.ExchangeRate> rates, CancellationToken cancellationToken)
        {
            var currencies = await db.Currencies.AsNoTracking().ToListAsync(cancellationToken);
            var dto = mapper.Map<List<ExchangeRateDto>>(rates);
            foreach (var item in dto)
            {
                item.FromCurrencyCode = currencies.FirstOrDefault(c => c.Id == item.FromCurrencyId)?.Code ?? string.Empty;
                item.ToCurrencyCode = currencies.FirstOrDefault(c => c.Id == item.ToCurrencyId)?.Code ?? string.Empty;
            }
            return dto;
        }
    }
}