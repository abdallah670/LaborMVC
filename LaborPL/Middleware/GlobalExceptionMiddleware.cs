using System.Net;
using System.Text.Json;

namespace LaborPL.Middleware
{
    /// <summary>
    /// Global exception handling middleware for consistent error responses
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred while processing request. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);
                
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var (statusCode, message, errors) = exception switch
            {
                // Validation errors
                FluentValidation.ValidationException ex => (
                    (int)HttpStatusCode.BadRequest,
                    "Validation failed",
                    ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList()
                ),
                
                // Unauthorized access
                UnauthorizedAccessException => (
                    (int)HttpStatusCode.Unauthorized,
                    "Unauthorized access",
                    null
                ),
                
                // Not found
                KeyNotFoundException => (
                    (int)HttpStatusCode.NotFound,
                    "Resource not found",
                    null
                ),
                
                // Conflict/Concurrency
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                    (int)HttpStatusCode.Conflict,
                    "The resource was modified by another user. Please refresh and try again.",
                    null
                ),
                
                // Invalid operation
                InvalidOperationException => (
                    (int)HttpStatusCode.BadRequest,
                    exception.Message,
                    null
                ),
                
                // Argument errors
                ArgumentException => (
                    (int)HttpStatusCode.BadRequest,
                    exception.Message,
                    null
                ),
                
                // Default: Internal server error
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred",
                    null
                )
            };

            context.Response.StatusCode = statusCode;

            var response = new
            {
                StatusCode = statusCode,
                Message = message,
                Errors = errors,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow,
#if DEBUG
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
#endif
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsJsonAsync(response, options);
        }
    }

    /// <summary>
    /// Extension method to add global exception middleware
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
