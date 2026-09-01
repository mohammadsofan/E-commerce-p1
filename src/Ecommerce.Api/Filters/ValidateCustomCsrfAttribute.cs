using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace Ecommerce.Api.Filters
{
    /// <summary>
    /// Enforces custom double-submit CSRF protection by verifying that the
    /// <c>X-XSRF-TOKEN</c> request header matches the <c>XSRF-TOKEN</c> cookie
    /// using a constant-time comparison.  Apply this attribute to every
    /// state-changing action (POST / PUT / DELETE) that is not already
    /// protected by ASP.NET's built-in antiforgery middleware.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ValidateCustomCsrfAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string CsrfCookieName = "XSRF-TOKEN";
        private const string CsrfHeaderName = "X-XSRF-TOKEN";

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!IsValidCsrfRequest(context))
            {
                context.Result = new ForbidResult();
            }
            return Task.CompletedTask;
        }

        private static bool IsValidCsrfRequest(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;

            var cookieToken = request.Cookies[CsrfCookieName];
            var headerToken = request.Headers[CsrfHeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken))
                return false;

            var cookieBytes = Encoding.UTF8.GetBytes(cookieToken);
            var headerBytes = Encoding.UTF8.GetBytes(headerToken);

            return cookieBytes.Length == headerBytes.Length
                   && CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
        }
    }
}
