using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LaborPL.Middleware
{
    /// <summary>
    /// Middleware to add security headers to all HTTP responses
    /// Protects against XSS, clickjacking, MIME-type sniffing, and other common attacks
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
            // Prevent MIME-type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking attacks
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // Enable XSS protection in browsers
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Control referrer information
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content Security Policy - restricts sources of content
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://js.stripe.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "frame-src https://js.stripe.com https://hooks.stripe.com; " +
                "connect-src 'self' https://api.stripe.com;";

            // Permissions Policy - controls browser features
            context.Response.Headers["Permissions-Policy"] = 
                "camera=(), microphone=(), geolocation=(self), payment=(self)";

            // Remove server header to prevent information disclosure
            context.Response.Headers.Remove("Server");

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to easily register the SecurityHeadersMiddleware
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
