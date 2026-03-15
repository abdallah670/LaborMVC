using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LaborPL.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // headers الأمان الأساسية
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN"; context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // ✅ السياسة الجديدة - بتسمح بكل حاجة عشان الشات يشتغل
            context.Response.Headers["Content-Security-Policy"] =
       "default-src * data: blob: filesystem: about: ws: wss: http: https: 'unsafe-inline' 'unsafe-eval'; " +
       "frame-src * https://js.stripe.com https://hooks.stripe.com;";
            // شيل الـ Server header
            context.Response.Headers.Remove("Server");

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}