using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LaborPL.Middleware
{
    /// <summary>
    /// Middleware for detailed audit logging of all HTTP requests and responses
    /// Tracks: User ID, IP Address, Request Path, Method, Status Code, Duration, Request/Response Body (sanitized)
    /// </summary>
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for static files and health checks
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path?.Contains("/health") == true || 
                path?.Contains("/hangfire") == true ||
                path?.StartsWith("/css/") == true ||
                path?.StartsWith("/js/") == true ||
                path?.StartsWith("/lib/") == true)
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var traceId = context.TraceIdentifier;
            var userId = context.User?.Identity?.IsAuthenticated == true 
                ? context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name 
                : "Anonymous";
            var ipAddress = GetClientIpAddress(context);
            var requestMethod = context.Request.Method;
            var requestPath = context.Request.Path;
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Log request started
            _logger.LogInformation(
                "[AUDIT] Request Started | TraceId: {TraceId} | Method: {Method} | Path: {Path} | User: {UserId} | IP: {IPAddress} | UserAgent: {UserAgent}",
                traceId, requestMethod, requestPath, userId, ipAddress, userAgent);

            // Capture request body for POST/PUT/PATCH
            string requestBody = string.Empty;
            if (IsLoggableMethod(requestMethod) && context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                
                // Sanitize sensitive data
                requestBody = SanitizeSensitiveData(requestBody);
            }

            // Capture response
            var originalBodyStream = context.Response.Body;
            string responseBody = string.Empty;
            
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            Exception? exception = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Read response body
                context.Response.Body.Position = 0;
                using var reader = new StreamReader(context.Response.Body);
                responseBody = await reader.ReadToEndAsync();
                context.Response.Body.Position = 0;
                
                // Copy back to original stream
                await responseBodyStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                // Sanitize response
                responseBody = SanitizeSensitiveData(responseBody);

                var statusCode = context.Response.StatusCode;
                var duration = stopwatch.ElapsedMilliseconds;

                // Log based on status code
                if (exception != null)
                {
                    _logger.LogError(
                        "[AUDIT] Request Failed | TraceId: {TraceId} | Method: {Method} | Path: {Path} | User: {UserId} | IP: {IPAddress} | Status: {StatusCode} | Duration: {Duration}ms | Exception: {Exception}",
                        traceId, requestMethod, requestPath, userId, ipAddress, statusCode, duration, exception.Message);
                }
                else if (statusCode >= 400)
                {
                    _logger.LogWarning(
                        "[AUDIT] Request Completed with Error | TraceId: {TraceId} | Method: {Method} | Path: {Path} | User: {UserId} | IP: {IPAddress} | Status: {StatusCode} | Duration: {Duration}ms | RequestBody: {RequestBody} | ResponseBody: {ResponseBody}",
                        traceId, requestMethod, requestPath, userId, ipAddress, statusCode, duration, 
                        Truncate(requestBody, 500), Truncate(responseBody, 500));
                }
                else
                {
                    _logger.LogInformation(
                        "[AUDIT] Request Completed | TraceId: {TraceId} | Method: {Method} | Path: {Path} | User: {UserId} | IP: {IPAddress} | Status: {StatusCode} | Duration: {Duration}ms | RequestBody: {RequestBody} | ResponseBody: {ResponseBody}",
                        traceId, requestMethod, requestPath, userId, ipAddress, statusCode, duration,
                        Truncate(requestBody, 500), Truncate(responseBody, 500));
                }
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded headers (if behind proxy)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private static bool IsLoggableMethod(string method)
        {
            return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeSensitiveData(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove common sensitive fields
            var sensitiveFields = new[]
            {
                "password", "Password", "confirmPassword", "oldPassword", "newPassword",
                "token", "Token", "authToken", "refreshToken", "accessToken",
                "secret", "Secret", "apiSecret", "apiKey",
                "creditCard", "cardNumber", "cvv", "cvc", "ssn", "socialSecurity"
            };

            var result = content;
            foreach (var field in sensitiveFields)
            {
                // Replace values that appear in JSON format like "password": "value"
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                var replacement = $"\"{field}\": \"[REDACTED]\"";
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    pattern,
                    replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Replace values in query strings
                var qsPattern = $"({field})=([^&]*)";
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    qsPattern,
                    "$1=[REDACTED]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return result;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength) + "...[truncated]";
        }
    }

    /// <summary>
    /// Extension method to easily register the AuditLoggingMiddleware
    /// </summary>
    public static class AuditLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuditLoggingMiddleware>();
        }
    }
}
