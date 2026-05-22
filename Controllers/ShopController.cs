using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using meow.Models;
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

        public ShopController(LibraryDbContext context)
        {
            _context = context;
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
                ViewBag.WybranyGatunek = $"Książki z kategorii: {gatunek}";
            }
            else
            {
                ViewBag.WybranyGatunek = string.IsNullOrEmpty(fraza)
                    ? "Wszystkie pozycje w e-księgarni meow 🐾"
                    : $"Wyniki wyszukiwania dla frazy: „{fraza}”";
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
                TempData["Message"] = "Niestety, ten produkt nie jest obecnie dostępny w sprzedaży.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            List<int> cartItems = string.IsNullOrEmpty(cartString)
                ? new List<int>()
                : cartString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

            if (cartItems.Count(id => id == bookId) >= book.IloscDoSprzedazy)
            {
                TempData["Message"] =
                    $"Osiągnięto limit! W magazynie meow mamy już tylko {book.IloscDoSprzedazy} szt. tego produktu.";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Details", new { id = bookId });
            }

            cartItems.Add(bookId);
            HttpContext.Session.SetString("Koszyk", string.Join(",", cartItems));

            TempData["Message"] = $"Pomyślnie dodano „{book.Tytul}” do Twojego koszyka! 🐾";
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
                TempData["Message"] =
                    "Automatyczna korekta: Niektóre produkty w koszyku zostały wyprzedane lub ich ilość w magazynie uległa zmianie. 🐾";
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

            TempData["Message"] = "Zaktualizowano zawartość koszyka.";
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
        public IActionResult FinalizeOrder(string metodaDostawy, decimal kosztDostawy, string metodaPlatnosci,
            string? kodBlik)
        {
            var sessionUser = HttpContext.Session.GetString("User");
            int? sessionClientId = HttpContext.Session.GetInt32("UserId");

            // 1. BEZPIECZEŃSTWO: Jeśli użytkownik nie jest zalogowany, nie pozwalamy na finalizację
            if (string.IsNullOrEmpty(sessionUser) || !sessionClientId.HasValue)
            {
                TempData["Message"] = "Musisz być zalogowany, aby sfinalizować zamówienie.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Account");
            }

            int finalKlientId = sessionClientId.Value;

            // 2. WALIDACJA KOSZYKA: Sprawdzenie, czy koszyk nie jest pusty
            var cartString = HttpContext.Session.GetString("Koszyk") ?? "";
            if (string.IsNullOrEmpty(cartString))
            {
                TempData["Message"] = "Twój koszyk był pusty lub zamówienie zostało już przetworzone.";
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

                // Generujemy JEDEN wspólny numer paczki dla CAŁEGO zamówienia
                string wspólnyNumerPaczki = "MEOW-" + random.Next(100000000, 999999999).ToString();

                // Opcjonalnie: Tutaj możesz wyciągnąć zapisany adres z sesji, jeśli Twoja tabela Zamowienie go obsługuje:
                // string? adres = HttpContext.Session.GetString("AdresDostawy");

                foreach (var kp in zakupioneGrupy)
                {
                    var ksiazka = _context.Books.FirstOrDefault(b => b.Id == kp.Key);
                    if (ksiazka != null)
                    {
                        if (ksiazka.IloscDoSprzedazy < kp.Value)
                        {
                            TempData["Message"] =
                                $"Przepraszamy, produkt „{ksiazka.Tytul}” wyprzedał się w międzyczasie.";
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
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                // Czyszczenie danych po udanej transakcji
                HttpContext.Session.Remove("Koszyk");
                HttpContext.Session.Remove("AdresDostawy");

                TempData["Message"] = "🐾 Sukces! Zamówienie zostało pomyślnie złożone w sklepie meow.";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                TempData["Message"] = "Błąd systemu zamówień: " + ex.Message;
                TempData["MessageType"] = "error";
                return RedirectToAction("Cart");
            }

            return RedirectToAction("Profile", "Account");
        }
    }
}