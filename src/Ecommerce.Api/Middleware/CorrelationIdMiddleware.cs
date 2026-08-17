using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Ecommerce.Api.Middleware
{
    /// <summary>
    /// Ensures every request has a correlation id: reads an incoming
    /// X-Correlation-Id header (when present) or generates a new one,
    /// echoes it on the response, exposes it via HttpContext.TraceIdentifier,
    /// and enriches structured logs so all log entries for a request share
    /// the same correlation id.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public const string HeaderName = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ReadOrGenerate(context);

            // Expose via TraceIdentifier (used by request logging) and in Items
            context.TraceIdentifier = correlationId;
            context.Items["CorrelationId"] = correlationId;

            // Echo on the response
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = correlationId;
            }

            // Enrich all Serilog log events in this request context
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private static string ReadOrGenerate(HttpContext context)
        {
            var incoming = context.Request.Headers.TryGetValue(HeaderName, out var values)
                ? values.ToString()
                : null;

            if (!string.IsNullOrWhiteSpace(incoming))
            {
                return incoming.Trim();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}