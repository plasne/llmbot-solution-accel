using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class HttpException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class HttpExceptionMiddleware(RequestDelegate next, ILogger<HttpExceptionMiddleware> logger)
{
    private readonly RequestDelegate next = next;
    private readonly ILogger<HttpExceptionMiddleware> logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this.next(context);
        }
        catch (HttpException ex)
        {
            this.logger.LogError(ex, "HTTP exception...");
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(ex.Message);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "internal server exception...");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(ex.Message);
        }
    }
}
