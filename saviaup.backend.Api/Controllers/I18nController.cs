using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaviaUp.Backend.Shared.Localization;

namespace SaviaUp.Backend.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/i18n")]
public sealed class I18nController : ControllerBase
{
    [HttpGet("{language}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Get(string language)
        => Ok(TranslationCatalog.Get(language));
}
