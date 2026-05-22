using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace meow.Models
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MeowAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string? _wymaganaRola;

        // Konstruktor bez parametrów - dla zwykłego zalogowania
        public MeowAuthorizeAttribute()
        {
            _wymaganaRola = null;
        }

        // Konstruktor z parametrem - dla konkretnej roli, np. [MeowAuthorize("Admin")]
        public MeowAuthorizeAttribute(string wymaganaRola)
        {
            _wymaganaRola = wymaganaRola;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var uzytkownik = session.GetString("User");
            var rolaUzytkownika = session.GetString("UserRole");

            // 1. Brak zalogowania -> idziesz do ekranu logowania
            if (string.IsNullOrEmpty(uzytkownik))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // 2. Jesteś zalogowany, ale sprawdzamy rolę (np. szukamy Admina, a Ty jesteś Klientem)
            if (!string.IsNullOrEmpty(_wymaganaRola) && rolaUzytkownika != _wymaganaRola)
            {
                // Tutaj możemy bezpiecznie użyć standardowego TempData, bo sesja działa!
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["Message"] = "Brak uprawnień do wyświetlenia tej sekcji! 🐾";
                    controller.TempData["MessageType"] = "error";
                }

                context.Result = new RedirectToActionResult("Index", "Shop", null);
            }

            base.OnActionExecuting(context);
        }
    }
}