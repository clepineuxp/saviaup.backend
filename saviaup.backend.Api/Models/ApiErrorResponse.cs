namespace SaviaUp.Backend.Api.Models;

public sealed record ApiErrorBody(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null);
public sealed record ApiErrorResponse(bool Success, ApiErrorBody Error)
{
    public static ApiErrorResponse Create(string code, string message, IReadOnlyDictionary<string, string[]>? details = null)
        => new(false, new ApiErrorBody(code, message, details));
}
