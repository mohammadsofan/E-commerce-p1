using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Commands.Admin
{
    public class CreateCurrencyCommandHandler : ICommandHandler<CreateCurrencyCommand, CurrencyDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateCurrencyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CurrencyDto> Handle(CreateCurrencyCommand command, CancellationToken cancellationToken = default)
        {
            var code = command.Code.Trim().ToUpperInvariant();
            if (code.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z]{3}$"))
                throw new DomainException("Currency code must be exactly 3 uppercase letters");

            var existing = await _db.Currencies
                .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
            if (existing != null)
                throw new DomainException($"Currency with code {code} already exists");

            var currency = new Currency
            {
                Id = Guid.NewGuid(),
                Code = code,
                Symbol = command.Symbol,
                IsBaseCurrency = command.IsBaseCurrency
            };

            if (currency.IsBaseCurrency)
            {
                var ordersExist = await _db.Orders.AnyAsync(cancellationToken);
                if (ordersExist)
                    throw new DomainException("Cannot change base currency because orders already exist.");
                await ClearBaseCurrencyAsync(cancellationToken);
            }

            _db.Currencies.Add(currency);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CurrencyDto>(currency);
        }

        protected async Task ClearBaseCurrencyAsync(CancellationToken cancellationToken)
        {
            var bases = await _db.Currencies.Where(c => c.IsBaseCurrency).ToListAsync(cancellationToken);
            foreach (var b in bases)
                b.IsBaseCurrency = false;
        }
    }

    public class UpdateCurrencyCommandHandler : ICommandHandler<UpdateCurrencyCommand, CurrencyDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateCurrencyCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<CurrencyDto> Handle(UpdateCurrencyCommand command, CancellationToken cancellationToken = default)
        {
            var currency = await _db.Currencies
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
            if (currency == null)
                throw new DomainException("Currency not found");

            var code = command.Code.Trim().ToUpperInvariant();
            if (code.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z]{3}$"))
                throw new DomainException("Currency code must be exactly 3 uppercase letters");

            var conflict = await _db.Currencies
                .FirstOrDefaultAsync(c => c.Code == code && c.Id != command.Id, cancellationToken);
            if (conflict != null)
                throw new DomainException($"Currency with code {code} already exists");

            currency.Code = code;
            currency.Symbol = command.Symbol;

            if (command.IsBaseCurrency && !currency.IsBaseCurrency)
            {
                var ordersExist = await _db.Orders.AnyAsync(cancellationToken);
                if (ordersExist)
                    throw new DomainException("Cannot change base currency because orders already exist.");

                var bases = await _db.Currencies.Where(c => c.IsBaseCurrency && c.Id != currency.Id).ToListAsync(cancellationToken);
                foreach (var b in bases)
                    b.IsBaseCurrency = false;
                currency.IsBaseCurrency = true;
            }
            else if (!command.IsBaseCurrency)
            {
                currency.IsBaseCurrency = false;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CurrencyDto>(currency);
        }
    }

    public class DeleteCurrencyCommandHandler : ICommandHandler<DeleteCurrencyCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteCurrencyCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteCurrencyCommand command, CancellationToken cancellationToken = default)
        {
            var currency = await _db.Currencies
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
            if (currency == null)
                throw new DomainException("Currency not found");

            if (currency.IsBaseCurrency)
                throw new DomainException("Cannot delete the base currency");

            var hasRates = await _db.ExchangeRates
                .AnyAsync(r => r.FromCurrencyId == command.Id || r.ToCurrencyId == command.Id, cancellationToken);
            if (hasRates)
                throw new DomainException("Cannot delete a currency that has exchange rates");

            _db.Currencies.Remove(currency);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }

    public class CreateExchangeRateCommandHandler : ICommandHandler<CreateExchangeRateCommand, ExchangeRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public CreateExchangeRateCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ExchangeRateDto> Handle(CreateExchangeRateCommand command, CancellationToken cancellationToken = default)
        {
            if (command.FromCurrencyId == command.ToCurrencyId)
                throw new DomainException("From and To currencies must differ");
            if (command.Rate <= 0)
                throw new DomainException("Rate must be positive");

            var rate = new ExchangeRate
            {
                Id = Guid.NewGuid(),
                FromCurrencyId = command.FromCurrencyId,
                ToCurrencyId = command.ToCurrencyId,
                Rate = command.Rate,
                EffectiveAt = command.EffectiveAt ?? DateTimeOffset.UtcNow
            };

            _db.ExchangeRates.Add(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ExchangeRateDto>(rate);
        }
    }

    public class UpdateExchangeRateCommandHandler : ICommandHandler<UpdateExchangeRateCommand, ExchangeRateDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UpdateExchangeRateCommandHandler(IApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<ExchangeRateDto> Handle(UpdateExchangeRateCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ExchangeRates
                .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
            if (rate == null)
                throw new DomainException("Exchange rate not found");

            if (command.FromCurrencyId == command.ToCurrencyId)
                throw new DomainException("From and To currencies must differ");
            if (command.Rate <= 0)
                throw new DomainException("Rate must be positive");

            rate.FromCurrencyId = command.FromCurrencyId;
            rate.ToCurrencyId = command.ToCurrencyId;
            rate.Rate = command.Rate;
            rate.EffectiveAt = command.EffectiveAt ?? DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ExchangeRateDto>(rate);
        }
    }

    public class DeleteExchangeRateCommandHandler : ICommandHandler<DeleteExchangeRateCommand, Unit>
    {
        private readonly IApplicationDbContext _db;

        public DeleteExchangeRateCommandHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Unit> Handle(DeleteExchangeRateCommand command, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ExchangeRates
                .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
            if (rate == null)
                throw new DomainException("Exchange rate not found");

            _db.ExchangeRates.Remove(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}