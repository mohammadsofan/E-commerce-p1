using System;
using System.Net;
using System.Text.Json;
using Ecommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Api.Middleware
{
    /// <summary>
    /// Centralized exception handling that returns RFC 7807 ProblemDetails responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            IHostEnvironment environment,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteProblemDetailsAsync(context, ex);
            }
        }

        private async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
        {
            // More-derived types must be matched before their base types.
            var (status, title) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                ConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict"),
                DomainException => (HttpStatusCode.BadRequest, "Domain Error"),
                _ => (HttpStatusCode.InternalServerError, "Server Error"),
            };

            var isServerError = status == HttpStatusCode.InternalServerError;
            var correlationId = context.TraceIdentifier;

            if (isServerError)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path} (correlationId: {CorrelationId})",
                    context.Request.Method, context.Request.Path, correlationId);
            }

            // Expected (domain) failures carry a customer-facing message. Unexpected failures
            // must never leak provider/SQL/stack detail outside Development.
            string detail;
            if (!isServerError)
            {
                detail = ex.Message;
            }
            else if (_environment.IsDevelopment())
            {
                detail = ex.Message + (ex.InnerException != null ? " Inner: " + ex.InnerException.Message : string.Empty);
            }
            else
            {
                detail = $"An unexpected error occurred. Reference: {correlationId}";
            }

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path.Value
            };
            problem.Extensions["correlationId"] = correlationId;

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
        }
    }
}
