using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace meow.Controllers
{
    public class CultureController : Controller
    {
        [HttpGet("Culture/Set/{culture}")]
        public IActionResult Set(string culture, string? returnUrl)
        {
            if (culture is not ("pl" or "en"))
                culture = "pl";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = Request.Path + Request.QueryString;

            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = "/";

            return LocalRedirect(returnUrl);
        }
    }
}
