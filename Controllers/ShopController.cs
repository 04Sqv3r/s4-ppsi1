using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using meow.Models;
using meow.Resources;
using meow.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace meow.Controllers
{
    public class ShopController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<ShopController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ShopController(
            LibraryDbContext context,
            EmailService emailService,
            ILogger<ShopController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _localizer = localizer;
        }

        // ==========================================================
        // 1. KATALOG PRODUKTÓW (FILTROWANIE + SORTOWANIE)
        // ==========================================================
        public IActionResult Index(string? gatunek, string? sortowanie, string? fraza)
        {
            var query = _context.Books.AsQueryable();

            // Filtrowanie po frazie z głównego paska menu
            if (!string.IsNullOrEmpty(fraza))
            {
                query = query.Where(b => b.Tytul!.Contains(fraza) || b.Autor!.Contains(fraza));
                ViewBag.AktualneWyszukiwanie = fraza;
            }

            if (!string.IsNullOrEmpty(gatunek))
            {
                query = query.Where(b => b.Gatunek == gatunek);
                ViewBag.WybranyGatunek = _localizer["Shop_Category", gatunek].Value;
            }
            else
            {
                ViewBag.WybranyGatunek = string.IsNullOrEmpty(fraza)
                    ? _localizer["Shop_AllBooks"].Value
                    : _localizer["Shop_SearchResults", fraza].Value;
            }

            switch (sortowanie)
            {
                case "cena_rosnaco":
                    query = query.OrderBy(b => b.Cena);
                    break;
                case "cena_malejaco":
                    query = query.OrderByDescending(b => b.Cena);
                    break;
                case "alfabetycznie":
                    query = query.OrderBy(b => b.Tytul);
                    break;
                default:
                    query = query.OrderBy(b => b.Id);
                    break;
            }

            ViewBag.AktualnyGatunek = gatunek;
            ViewBag.AktualneSortowanie = sortowanie;

            return View(query.ToList());
        }

        // ==========================================================
        // 2. KARTA SZCZEGÓŁÓW PRODUKTU
        // ==========================================================
        public IActionResult Details(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == id);
            if (book == null) return NotFound();

            var wypozyczoneEgzemplarzeIds = _context.Wypozyczenia
                .Where(w => w.DataZwrotu == null && w.IdEgzemplarz != null)
                .Select(w => w.IdEgzemplarz).ToList();

            var wolneEgzemplarze = _context.Egzemplarze
                .Where(e => e.Book != null && e.Book.Id == id && !wypozyczoneEgzemplarzeIds.Contains(e.IdEgzemplarza))
                .ToList();

            ViewBag.WolneEgzemplarze = wolneEgzemplarze;
            ViewBag.DostepneDoWypozyczenia = wolneEgzemplarze.Count;

            return View(book);
        }

        // ==========================================================
        // 3. DODAWANIE DO KOSZYKA (STRING FORMAT)
        // ==========================================================
        [HttpPost]
        public IActionResult AddToCart(int bookId)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);
            if (book == null || book.Cena == 0 || book.IloscDoSprzedazy <= 0)
            {
                TempData["Message"] = _localizer["Msg_ProductUnavailable"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            List<int> cartItems = string.IsNullOrEmpty(cartString)
                ? new List<int>()
                : cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            if (cartItems.Count(id => id == bookId) >= book.IloscDoSprzedazy)
            {
                TempData["Message"] = string.Format(_localizer["Msg_CartLimit"].Value, book.IloscDoSprzedazy);
                TempData["MessageType"] = "warning";
                return RedirectToAction("Details", new { id = bookId });
            }

            cartItems.Add(bookId);
            HttpContext.Session.SetString("Koszyk", string.Join(",", cartItems));

            TempData["Message"] = string.Format(_localizer["Msg_AddedToCart"].Value, book.Tytul);
            TempData["MessageType"] = "success";
            return RedirectToAction("Details", new { id = bookId });
        }

        // ==========================================================
        // 4. KOSZYK (Z INTEGRACJĄ KOREKTY STANU W LOCIE)
        // ==========================================================
        public IActionResult Cart()
        {
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (string.IsNullOrEmpty(cartString)) return View(new List<Book>());

            var bookIds = cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var booksInCart = _context.Books.Where(b => bookIds.Contains(b.Id)).ToList();

            // --- INTELIGENTNA KOREKTA STANU MAGAZYNOWEGO W KOSZYKU ---
            bool dokonanoKorekty = false;
            var zaktualizowanyKoszyk = new List<int>();

            foreach (var id in bookIds)
            {
                var ksiazka = booksInCart.FirstOrDefault(b => b.Id == id);
                if (ksiazka != null && ksiazka.IloscDoSprzedazy > 0)
                {
                    if (zaktualizowanyKoszyk.Count(x => x == id) < ksiazka.IloscDoSprzedazy)
                    {
                        zaktualizowanyKoszyk.Add(id);
                    }
                    else
                    {
                        dokonanoKorekty = true;
                    }
                }
                else
                {
                    dokonanoKorekty = true;
                }
            }

            if (dokonanoKorekty)
            {
                TempData["Message"] = _localizer["Msg_CartAutoFix"].Value;
                TempData["MessageType"] = "warning";
                HttpContext.Session.SetString("Koszyk", string.Join(",", zaktualizowanyKoszyk));
                bookIds = zaktualizowanyKoszyk;
                booksInCart = _context.Books.Where(b => bookIds.Contains(b.Id)).ToList();
            }

            if (!bookIds.Any()) return View(new List<Book>());

            ViewBag.Quantities = bookIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            decimal suma = 0;
            foreach (var book in booksInCart)
            {
                suma += (book.Cena ?? 0) * bookIds.Count(id => id == book.Id);
            }

            ViewBag.SumaKoszyka = suma;
            return View(booksInCart);
        }

        // ==========================================================
        // 5. USUWANIE Z KOSZYKA
        // ==========================================================
        [HttpPost]
        public IActionResult RemoveFromCart(int bookId)
        {
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (!string.IsNullOrEmpty(cartString))
            {
                var bookIds = cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                bookIds.Remove(bookId);
                if (bookIds.Any()) HttpContext.Session.SetString("Koszyk", string.Join(",", bookIds));
                else HttpContext.Session.Remove("Koszyk");
            }

            TempData["Message"] = _localizer["Msg_CartUpdated"].Value;
            TempData["MessageType"] = "success";
            return RedirectToAction("Cart");
        }

// ==========================================================
        // 6. CHECKOUT (POWRAZANY Z UserId Z SESJI)
        // ==========================================================
        [HttpGet]
        [MeowAuthorize]
        public IActionResult Checkout()
        {
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (string.IsNullOrEmpty(cartString)) return RedirectToAction("Cart");

            int? idKlienta = HttpContext.Session.GetInt32("UserId");
            if (idKlienta == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var klientData = _context.Klienci.FirstOrDefault(k => k.IdKlienta == idKlienta.Value);
            if (klientData == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Wyliczenie wartości koszyka
            var bookIds = cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var booksInCart = _context.Books.Where(b => bookIds.Contains(b.Id)).ToList();

            decimal suma = 0;
            foreach (var book in booksInCart)
            {
                suma += (book.Cena ?? 0) * bookIds.Count(id => id == book.Id);
            }

            ViewBag.WartoscProduktow = suma;
            ViewBag.KosztDostawy = 0.00m;
            ViewBag.WartoscKoszyka = suma;

            return View(klientData);
        }

        // ==========================================================
        // 7. METODA DOSTAWY (ZAPISZ DANE DO SESJI!)
        // ==========================================================
        [HttpPost]
        [MeowAuthorize]
        public IActionResult Delivery(string typ_odbiorcy, string imie, string? nazwisko, string? nazwa_firmy,
            string? nip, string email, string telefon, string kraj, string ulica, string numer, string? lokal,
            string kodPocztowy, string miejscowosc)
        {
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (string.IsNullOrEmpty(cartString)) return RedirectToAction("Cart");

            var bookIds = cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var booksInCart = _context.Books.Where(b => bookIds.Contains(b.Id)).ToList();

            decimal suma = 0;
            foreach (var book in booksInCart)
            {
                suma += (book.Cena ?? 0) * bookIds.Count(id => id == book.Id);
            }

            ViewBag.WartoscProduktow = suma;
            ViewBag.WartoscKoszyka = suma;

            // NAPRAWA: Zapisujemy sformatowany adres w sesji, by FinalizeOrder mógł go w przyszłości powiązać/wykorzystać
            string pelnyAdres =
                $"{imie} {nazwisko}. ul. {ulica} {numer}{(string.IsNullOrEmpty(lokal) ? "" : "/" + lokal)}, {kodPocztowy} {miejscowosc}. Tel: {telefon}";
            HttpContext.Session.SetString("AdresDostawy", pelnyAdres);

            return View();
        }

        // ==========================================================
        // 8. OSTATECZNE FINALIZOWANIE ZAMÓWIENIA 
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> FinalizeOrder(string metodaDostawy, decimal kosztDostawy, string metodaPlatnosci,
            string? kodBlik)
        {
            var sessionUser = HttpContext.Session.GetString("User");
            int? sessionClientId = HttpContext.Session.GetInt32("UserId");

            // 1. BEZPIECZEŃSTWO: Jeśli użytkownik nie jest zalogowany, nie pozwalamy na finalizację
            if (string.IsNullOrEmpty(sessionUser) || !sessionClientId.HasValue)
            {
                TempData["Message"] = _localizer["Msg_LoginRequired"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Account");
            }

            int finalKlientId = sessionClientId.Value;

            // 2. WALIDACJA KOSZYKA: Sprawdzenie, czy koszyk nie jest pusty
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (string.IsNullOrEmpty(cartString))
            {
                TempData["Message"] = _localizer["Msg_CartEmpty"].Value;
                TempData["MessageType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            var bookIds = cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var zakupioneGrupy = bookIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            // 3. TRANSAKCJA BAZODANOWA: Bezpieczne modyfikowanie ilości i zapis zamówień
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                Random random = new Random();

                string wspólnyNumerPaczki = "MEOW-" + random.Next(100000000, 999999999).ToString();
                var pozycjeDoMaila = new List<string>();

                foreach (var kp in zakupioneGrupy)
                {
                    var ksiazka = _context.Books.FirstOrDefault(b => b.Id == kp.Key);
                    if (ksiazka != null)
                    {
                        if (ksiazka.IloscDoSprzedazy < kp.Value)
                        {
                            TempData["Message"] = string.Format(_localizer["Msg_ProductSoldOut"].Value, ksiazka.Tytul);
                            TempData["MessageType"] = "error";
                            transaction.Rollback();
                            return RedirectToAction("Cart");
                        }

                        ksiazka.IloscDoSprzedazy -= kp.Value;

                        for (int i = 0; i < kp.Value; i++)
                        {
                            var noweZamowienie = new Zamowienie
                            {
                                IdKlienta = finalKlientId,
                                DataZamowienia = DateTime.Now,
                                Status = "W przygotowaniu",
                                NumerSledzenia = wspólnyNumerPaczki, // Przypisujemy ten sam kod całej paczce
                                IdKsiazki = ksiazka.Id
                            };
                            _context.Zamowienia.Add(noweZamowienie);
                        }

                        pozycjeDoMaila.Add($"„{ksiazka.Tytul}” × {kp.Value} szt.");
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                var klient = _context.Klienci.FirstOrDefault(k => k.IdKlienta == finalKlientId);
                if (klient != null && !string.IsNullOrEmpty(klient.Email))
                {
                    try
                    {
                        await _emailService.SendOrderConfirmationAsync(
                            klient.Email,
                            $"{klient.Imie} {klient.Nazwisko}",
                            wspólnyNumerPaczki,
                            pozycjeDoMaila);
                    }
                    catch (Exception mailEx)
                    {
                        _logger.LogWarning(mailEx, "Zamówienie zapisane, ale e-mail nie został wysłany.");
                    }
                }

                HttpContext.Session.Remove("Koszyk");
                HttpContext.Session.Remove("AdresDostawy");

                TempData["Message"] = _localizer["Msg_OrderSuccess"].Value;
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Message"] = string.Format(_localizer["Msg_OrderError"].Value, ex.Message);
                TempData["MessageType"] = "error";
                return RedirectToAction("Cart");
            }

            return RedirectToAction("Profile", "Account");
        }
    }
}