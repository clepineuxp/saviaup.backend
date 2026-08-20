using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderNames.CorrelationId].ToString();
        var correlationId = Guid.TryParse(supplied, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString();
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderNames.CorrelationId] = correlationId;
        await next(context);
    }
}
