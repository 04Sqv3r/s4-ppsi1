using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using meow.Resources;

namespace meow.Models
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MeowAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _wymaganaRola;

        public MeowAuthorizeAttribute()
        {
            _wymaganaRola = null;
        }

        public MeowAuthorizeAttribute(string wymaganaRola)
        {
            _wymaganaRola = wymaganaRola;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var uzytkownik = session.GetString("User");
            var rolaUzytkownika = session.GetString("UserRole");

            if (string.IsNullOrEmpty(uzytkownik))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (!string.IsNullOrEmpty(_wymaganaRola) && rolaUzytkownika != _wymaganaRola)
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResources>>();
                    controller.TempData["Message"] = localizer["Msg_NoPermission"].Value;
                    controller.TempData["MessageType"] = "error";
                }

                context.Result = new RedirectToActionResult("Index", "Shop", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
