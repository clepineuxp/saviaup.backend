using SaviaUp.Backend.Api.Models;

namespace SaviaUp.Backend.Api.Middleware;

internal static class HttpResponseExtensions
{
    public static async Task WriteErrorAsync(this HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(code, message), context.RequestAborted);
    }
}
