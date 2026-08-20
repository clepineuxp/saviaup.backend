using SaviaUp.Backend.Shared.Constants;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {CorrelationId} was cancelled by the client.", context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error for request {CorrelationId}.", context.TraceIdentifier);
            if (!context.Response.HasStarted)
                await context.WriteErrorAsync(
                    StatusCodes.Status500InternalServerError,
                    ErrorCodes.Internal,
                    TranslationCatalog.Translate(LocalizationKeys.InternalError, context.Request.Headers.AcceptLanguage.ToString()));
        }
    }
}
