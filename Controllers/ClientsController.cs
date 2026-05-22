using Microsoft.AspNetCore.Mvc;
using meow.Models;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace meow.Controllers
{[MeowAuthorize("Admin")]
    public class ClientsController : Controller
    {
        private readonly LibraryDbContext _context;
        public ClientsController(LibraryDbContext context) { _context = context; }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Account");
            return View(_context.Klienci.ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Klient klient)
        {
            string czysteImie = Regex.Replace(klient.Imie, @"[^a-zA-ZąęćłńóśźżĄĘĆŁŃÓŚŹŻ]", "");
            string czysteNazwisko = Regex.Replace(klient.Nazwisko, @"[^a-zA-ZąęćłńóśźżĄĘĆŁŃÓŚŹŻ]", "");

            TextInfo textInfo = new CultureInfo("pl-PL", false).TextInfo;
            klient.Imie = textInfo.ToTitleCase(czysteImie.ToLower());
            klient.Nazwisko = textInfo.ToTitleCase(czysteNazwisko.ToLower());
            klient.Telefon = Regex.Replace(klient.Telefon, @"[^0-9]", "");

            if (string.IsNullOrEmpty(klient.Imie) || string.IsNullOrEmpty(klient.Nazwisko))
            {
                TempData["Message"] = "Błąd: Imię i nazwisko nie mogą być puste.";
                TempData["MessageType"] = "error";
                return View(klient);
            }

            _context.Klienci.Add(klient);
            _context.SaveChanges();

            TempData["Message"] = $"Klient {klient.Imie} {klient.Nazwisko} został zarejestrowany w systemie meow!";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index");
        }
    }
}