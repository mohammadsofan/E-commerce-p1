using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.Api.Middleware
{
    /// <summary>
    /// Adds security-related HTTP headers to every response to harden the API
    /// against common web attacks (clickjacking, MIME sniffing, XSS, etc.).
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Prevent the page from being rendered in a frame (clickjacking protection)
            headers["X-Frame-Options"] = "DENY";

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Control how much referrer information is sent
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict which browser features/powers can be used
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // Basic XSS filter for legacy browsers
            headers["X-XSS-Protection"] = "1; mode=block";

            await _next(context);
        }
    }
}