using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Queries.Admin;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ecommerce.Application.Tests
{
    public class CurrencyTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IMapper CreateMapper()
        {
            return new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfileForTests>();
            }).CreateMapper();
        }

        [Fact]
        public async Task CreateCurrency_SetsBaseCurrency()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCurrencyCommandHandler(ctx, CreateMapper());

            var result = await handler.Handle(new CreateCurrencyCommand { Code = "usd", Symbol = "$", IsBaseCurrency = true });

            Assert.Equal("USD", result.Code);
            Assert.True(result.IsBaseCurrency);
        }

        [Fact]
        public async Task CreateCurrency_DuplicateCode_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCurrencyCommandHandler(ctx, CreateMapper());
            await handler.Handle(new CreateCurrencyCommand { Code = "EUR", Symbol = "€", IsBaseCurrency = false });

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new CreateCurrencyCommand { Code = "eur", Symbol = "€", IsBaseCurrency = false }));
        }

        [Fact]
        public async Task CreateSecondBaseCurrency_ClearsFirstBase()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCurrencyCommandHandler(ctx, CreateMapper());
            var usd = await handler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });
            var eur = await handler.Handle(new CreateCurrencyCommand { Code = "EUR", Symbol = "€", IsBaseCurrency = true });

            var currencies = await ctx.Currencies.ToListAsync();
            // Assuming Option B: It clears the first base currency (in memory DB handles the logic, but doesn't throw DbUpdateException on Index).
            Assert.Single(currencies.Where(c => c.IsBaseCurrency));
            Assert.Equal("EUR", currencies.Single(c => c.IsBaseCurrency).Code);
        }

        [Fact]
        public async Task DeleteBaseCurrency_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new CreateCurrencyCommandHandler(ctx, CreateMapper());
            var usd = await handler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });

            var deleteHandler = new DeleteCurrencyCommandHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() => deleteHandler.Handle(new DeleteCurrencyCommand { Id = usd.Id }));
        }

        [Fact]
        public async Task CreateExchangeRate_AndConvert_UsesLatestRate()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currencyHandler = new CreateCurrencyCommandHandler(ctx, mapper);
            var usd = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });
            var eur = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "EUR", Symbol = "€", IsBaseCurrency = false });

            var rateHandler = new CreateExchangeRateCommandHandler(ctx, mapper);
            var oldRate = await rateHandler.Handle(new CreateExchangeRateCommand
            {
                FromCurrencyId = usd.Id,
                ToCurrencyId = eur.Id,
                Rate = 0.85m,
                EffectiveAt = DateTimeOffset.UtcNow.AddDays(-10)
            });
            var newRate = await rateHandler.Handle(new CreateExchangeRateCommand
            {
                FromCurrencyId = usd.Id,
                ToCurrencyId = eur.Id,
                Rate = 0.92m,
                EffectiveAt = DateTimeOffset.UtcNow.AddDays(-1)
            });

            var convertHandler = new ConvertCurrencyQueryHandler(ctx);
            var result = await convertHandler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "USD", To = "EUR" });

            Assert.Equal(0.92m, result.Rate);
            Assert.Equal(92m, result.ConvertedAmount);
            Assert.True(oldRate.EffectiveAt < newRate.EffectiveAt);
        }

        [Fact]
        public async Task Convert_SameCurrency_ReturnsIdentity()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new ConvertCurrencyQueryHandler(ctx);

            var result = await handler.Handle(new ConvertCurrencyQuery { Amount = 50m, From = "USD", To = "usd" });

            Assert.Equal(1m, result.Rate);
            Assert.Equal(50m, result.ConvertedAmount);
        }

        [Fact]
        public async Task Convert_UnknownCurrency_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var handler = new ConvertCurrencyQueryHandler(ctx);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(new ConvertCurrencyQuery { Amount = 50m, From = "USD", To = "XXX" }));
        }

        [Fact]
        public async Task Convert_NoRate_Throws()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currencyHandler = new CreateCurrencyCommandHandler(ctx, mapper);
            var usd = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });
            var jpy = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "JPY", Symbol = "¥", IsBaseCurrency = false });

            var convertHandler = new ConvertCurrencyQueryHandler(ctx);
            await Assert.ThrowsAsync<DomainException>(() =>
                convertHandler.Handle(new ConvertCurrencyQuery { Amount = 50m, From = "USD", To = "JPY" }));
        }

        [Fact]
        public async Task Convert_MissingRate_UsesInverseOfReverseRate()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currencyHandler = new CreateCurrencyCommandHandler(ctx, mapper);
            var usd = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });
            var gbp = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "GBP", Symbol = "£", IsBaseCurrency = false });

            var rateHandler = new CreateExchangeRateCommandHandler(ctx, mapper);
            await rateHandler.Handle(new CreateExchangeRateCommand
            {
                FromCurrencyId = gbp.Id,
                ToCurrencyId = usd.Id,
                Rate = 1.25m,
                EffectiveAt = DateTimeOffset.UtcNow
            });

            var convertHandler = new ConvertCurrencyQueryHandler(ctx);
            var result = await convertHandler.Handle(new ConvertCurrencyQuery { Amount = 100m, From = "USD", To = "GBP" });

            Assert.Equal(0.8m, result.Rate);
            Assert.Equal(80m, result.ConvertedAmount);
        }

        [Fact]
        public async Task GetCurrencies_ReturnsAllOrderedBaseFirst()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var handler = new CreateCurrencyCommandHandler(ctx, mapper);
            await handler.Handle(new CreateCurrencyCommand { Code = "EUR", Symbol = "€", IsBaseCurrency = false });
            await handler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });

            var queryHandler = new GetCurrenciesQueryHandler(ctx, mapper);
            var result = await queryHandler.Handle(new GetCurrenciesQuery());

            Assert.Equal(2, result.Count);
            Assert.Equal("USD", result[0].Code);
            Assert.Equal("EUR", result[1].Code);
        }

        [Fact]
        public async Task GetExchangeRates_ReturnsCodes()
        {
            using var ctx = CreateInMemoryContext();
            var mapper = CreateMapper();
            var currencyHandler = new CreateCurrencyCommandHandler(ctx, mapper);
            var usd = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "USD", Symbol = "$", IsBaseCurrency = true });
            var eur = await currencyHandler.Handle(new CreateCurrencyCommand { Code = "EUR", Symbol = "€", IsBaseCurrency = false });
            await new CreateExchangeRateCommandHandler(ctx, mapper).Handle(new CreateExchangeRateCommand
            {
                FromCurrencyId = usd.Id,
                ToCurrencyId = eur.Id,
                Rate = 0.9m,
                EffectiveAt = DateTimeOffset.UtcNow
            });

            var queryHandler = new GetExchangeRatesQueryHandler(ctx, mapper);
            var result = await queryHandler.Handle(new GetExchangeRatesQuery());

            Assert.Single(result);
            Assert.Equal("USD", result[0].FromCurrencyCode);
            Assert.Equal("EUR", result[0].ToCurrencyCode);
            Assert.Equal(0.9m, result[0].Rate);
        }
    }
}

