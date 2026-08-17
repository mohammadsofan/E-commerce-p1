using System;
using System.Net;
using System.Text.Json;
using Ecommerce.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Middleware
{
    /// <summary>
    /// Centralized exception handling that returns RFC 7807 ProblemDetails responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
        {
            // More-derived types must be matched before their base types.
            var (status, title) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
                ConcurrencyException => (HttpStatusCode.Conflict, "Concurrency Conflict"),
                DomainException => (HttpStatusCode.BadRequest, "Domain Error"),
                _ => (HttpStatusCode.InternalServerError, "Server Error"),
            };

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path.Value
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
        }
    }
}
