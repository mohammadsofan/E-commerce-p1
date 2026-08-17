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

            // Cross-origin isolation / embedding policy
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-site";

            // Content Security Policy - allow self-hosted resources and inline styles
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";

            await _next(context);
        }
    }
}