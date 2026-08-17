using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    public class CurrenciesController : ControllerBase
    {
        private readonly QueryDispatcher _queryDispatcher;

        public CurrenciesController(QueryDispatcher queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetCurrenciesQuery();
            var result = await _queryDispatcher.Send<GetCurrenciesQuery, List<CurrencyDto>>(query);
            return Ok(result);
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates()
        {
            var query = new GetExchangeRatesQuery();
            var result = await _queryDispatcher.Send<GetExchangeRatesQuery, List<ExchangeRateDto>>(query);
            return Ok(result);
        }

        [HttpGet("convert")]
        public async Task<IActionResult> Convert(
            [FromQuery] decimal amount,
            [FromQuery] string from = "USD",
            [FromQuery] string to = "EUR")
        {
            var query = new ConvertCurrencyQuery { Amount = amount, From = from, To = to };
            var result = await _queryDispatcher.Send<ConvertCurrencyQuery, CurrencyConversionResult>(query);
            return Ok(result);
        }
    }
}