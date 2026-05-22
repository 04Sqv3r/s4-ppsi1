using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using meow.Models;
using System.Linq;

namespace meow.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryDbContext _context;

        public HomeController(LibraryDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // INTELIGENTNA STRONA GŁÓWNA: ROZDZIELENIE SKLEPU OD ADMINA
        // ==========================================================
        public IActionResult Index()
        {
            var sessionRole = HttpContext.Session.GetString("UserRole");

            // Jeśli zalogowany użytkownik ma rolę Admina, pokazujemy mu kokpit zarządczy
            if (sessionRole == "Admin")
            {
                // Pobieramy szybkie statystyki do wyświetlenia na kafelkach dashboardu
                ViewBag.LiczbaKsiazek = _context.Books.Count();
                ViewBag.LiczbaEgzemplarzy = _context.Egzemplarze.Count();
                ViewBag.AktywneWypozyczenia = _context.Wypozyczenia.Count(w => w.DataZwrotu == null && w.IdEgzemplarz != null);
                ViewBag.NoweZamowieniaSklep = _context.Wypozyczenia.Count(w => w.DataZwrotu == null && w.IdEgzemplarz == null);
                ViewBag.LiczbaKlientow = _context.Klienci.Count();

                return View("AdminDashboard"); // Wywołujemy dedykowany plik widoku dla admina
            }

            // DLA ZWYKŁEGO UŻYTKOWNIKA / GOŚCIA: Ładujemy standardową stronę główną sklepu
            var nowosci = _context.Books.OrderByDescending(b => b.Id).Take(4).ToList();
            return View(nowosci);
        }

        public IActionResult Struktura()
        {
            return View();
        }
    }
}