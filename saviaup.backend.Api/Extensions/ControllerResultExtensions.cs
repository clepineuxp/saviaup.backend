using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Api.Models;
using SaviaUp.Backend.Domain.Results;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Extensions;

public static class ControllerResultExtensions
{
    public static ActionResult FromResult(this ControllerBase controller, Result result)
        => result.IsSuccess ? controller.NoContent() : controller.Error(result.Error!);

    public static ActionResult<T> FromResult<T>(this ControllerBase controller, Result<T> result)
        => result.IsSuccess ? controller.Ok(result.Value) : controller.Error(result.Error!);

    public static ActionResult Error(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthenticated => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Business => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        var language = controller.Request.Headers.AcceptLanguage.ToString();
        return controller.StatusCode(status, ApiErrorResponse.Create(error.Code, TranslationCatalog.Translate(error.MessageKey, language)));
    }
}
