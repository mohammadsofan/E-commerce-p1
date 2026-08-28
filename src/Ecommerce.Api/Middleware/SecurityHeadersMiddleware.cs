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

            // Content Security Policy. This is a JSON API: no inline script or plugin content
            // should ever execute from a response, and it must not be framed.
            // Swagger UI needs relaxed script/style rules, so it gets its own policy.
            headers["Content-Security-Policy"] = IsSwaggerRequest(context)
                ? "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; frame-ancestors 'none'"
                : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

            await _next(context);
        }

        private static bool IsSwaggerRequest(HttpContext context)
        {
            var path = context.Request.Path.Value;
            return path != null && path.StartsWith("/swagger", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}