using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using meow.Models;
using meow.Resources;
using System.Linq;
using System;
using System.Collections.Generic;
using BCrypt.Net;

namespace meow.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AccountController(LibraryDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // ==========================================================
        // 1. PROFIL UŻYTKOWNIKA (GET) - STRONA "MOJE KONTO"
        // ==========================================================
        [HttpGet]
        public IActionResult Profile()
        {
            // 1. Walidacja sesji
            var userLogin = HttpContext.Session.GetString("User");
            var clientId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole"); // Dodane dla sprawdzenia roli

            // --- ZABEZPIECZENIE DLA ADMINA (Oryginalna logika zachowana, dodany warunek) ---
            if (userRole == "Admin")
            {
                return RedirectToAction("Index", "Admin"); // Zakładając, że masz taki kontroler
            }

            if (string.IsNullOrEmpty(userLogin) || !clientId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Pobranie danych klienta z bazy
            var klient = _context.Klienci.FirstOrDefault(k => k.IdKlienta == clientId.Value);
            if (klient == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 3. Pobranie i zmapowanie kar finansowych (Punkt 2)
            var nienaliczoneKary = _context.Platnosci
                .Include(p => p.Wypozyczenie).ThenInclude(w => w!.Egzemplarz).ThenInclude(e => e!.Book)
                .Where(p => p.Wypozyczenie != null && p.Wypozyczenie.IdKlient == klient.IdKlienta)
                .ToList();

            var finesList = nienaliczoneKary.Select(p => new FineItemViewModel
            {
                Id = p.IdPlatnosc,
                BookTitle = p.Wypozyczenie?.Egzemplarz?.Book?.Tytul ?? _localizer["Profile_RegulatoryFee"].Value,
                Amount = p.Kwota,
                DateGenerated = p.Wypozyczenie?.DataZwrotu?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd")
            }).ToList();

            // 4. Budowanie pełnego modelu widoku profilu
            var model = new ProfileViewModel
            {
                CustomerName = $"{klient.Imie} {klient.Nazwisko}",
                
                // --- SEKCJA WYPOŻYCZEŃ ---
                Rentals = _context.Wypozyczenia
                    .Where(w => w.IdKlient == klient.IdKlienta && w.IdEgzemplarz != null)
                    .Select(w => new RentalHistoryItem
                    {
                        Id = w.IdWypozyczenie,
                        BookTitle = w.Egzemplarz != null && w.Egzemplarz.Book != null ? w.Egzemplarz.Book.Tytul : _localizer["Profile_UnknownTitle"].Value,
                        RentalDate = w.DataWypozyczenia.ToString("yyyy-MM-dd"),
                        Status = w.DataZwrotu.HasValue ? "Rozliczone" : (DateTime.Today > w.DataPlanowanegoZwrotu ? "Zaległość" : "Aktywne"),
                        ReturnDate = w.DataZwrotu.HasValue ? "Zwrócona" : "Wypożyczona"
                    }).ToList(), 

                // --- SEKCJA ZAMÓWIEŃ Z PEŁNYM DOSTĘPEM DO SZCZEGÓŁÓW ---
                Packages = _context.Zamowienia
                    .Include(z => z.Book)
                    .Where(z => z.IdKlienta == klient.IdKlienta)
                    .ToList()
                    .GroupBy(z => new { z.DataZamowienia, z.NumerSledzenia })
                    .Select(group => {
                        var pierwsze = group.First();
                        
                        var wewnętrznePozycje = group
                            .Where(z => z.Book != null)
                            .GroupBy(z => z.IdKsiazki)
                            .Select(bGroup => {
                                var ksiazka = bGroup.First().Book!;
                                return new OrderItemViewModel
                                {
                                    Title = ksiazka.Tytul,
                                    Author = ksiazka.Autor,
                                    Price = ksiazka.Cena ?? 0.00m,
                                    ImageUrl = ksiazka.ImageUrl ?? "/images/default-cover.jpg",
                                    Quantity = bGroup.Count()
                                };
                            }).ToList();

                        decimal łącznaSuma = wewnętrznePozycje.Sum(item => item.Price * item.Quantity);

                        return new OrderGroupViewModel
                        {
                            OrderId = pierwsze.Id,
                            OrderDate = pierwsze.DataZamowienia.ToString("yyyy-MM-dd HH:mm"),
                            Status = pierwsze.Status,
                            TrackingNumber = pierwsze.NumerSledzenia ?? _localizer["Profile_NoTracking"].Value,
                            TotalPrice = łącznaSuma,
                            Items = wewnętrznePozycje
                        };
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),

                // --- SEKCJA KAR I PŁATNOŚCI ---
                Fines = finesList,
                TotalFinesAmount = finesList.Sum(f => f.Amount)
            };

            return View(model);
        }

        // ==========================================================
        // 2. LOGOWANIE (GET)
        // ==========================================================
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // ==========================================================
        // 3. LOGOWANIE (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken] 
       
        public async Task<IActionResult> Login(string login, string haslo, string? returnUrl)
        {
            var user = await _context.Users
                .Include(u => u.Klient)
                .FirstOrDefaultAsync(u => u.Login == login); 

            if (user != null && BCrypt.Net.BCrypt.Verify(haslo, user.Haslo))
            {
                HttpContext.Session.SetString("User", user.Login ?? "Użytkownik");
                HttpContext.Session.SetString("UserRole", user.Rola ?? "Klient");

                if (user.KlientId.HasValue)
                    HttpContext.Session.SetInt32("UserId", user.KlientId.Value);
                else
                {
                    var powiazanyKlient = await _context.Klienci.FirstOrDefaultAsync(k => k.Email == user.Login);
                    if (powiazanyKlient != null)
                        HttpContext.Session.SetInt32("UserId", powiazanyKlient.IdKlienta);
                }

                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = _localizer["Err_InvalidLogin"].Value;
            return View();
        }

        // ==========================================================
        // 4. REJESTRACJA (GET)
        // ==========================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ==========================================================
        // 5. REJESTRACJA (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult Register([Bind("Login,Haslo,Email,Imie,Nazwisko,Telefon")] RegisterViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Telefon))
            {
                model.Telefon = new string(model.Telefon.Where(char.IsDigit).ToArray());
                if (model.Telefon.Length != 9)
                    ModelState.AddModelError(nameof(model.Telefon), _localizer["Register_ValidationPhone"].Value);
            }
            else
            {
                model.Telefon = string.Empty;
                ModelState.Remove(nameof(model.Telefon));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Error = _localizer["Err_InvalidFields"].Value;
                return View(model);
            }
            if (_context.Users.Any(u => u.Login == model.Login))
            {
                ViewBag.Error = _localizer["Err_UserExists"].Value;
                return View(model);
            }

            if (_context.Klienci.Any(k => k.Email == model.Email))
            {
                ViewBag.Error = _localizer["Err_EmailTaken"].Value;
                return View(model);
            }

            var newKlient = new Klient
            {
                Imie = model.Imie,
                Nazwisko = model.Nazwisko,
                Email = model.Email,
                Telefon = string.IsNullOrEmpty(model.Telefon) ? "-" : model.Telefon
            };

            _context.Klienci.Add(newKlient);
            _context.SaveChanges(); 

            var hashedHaslo = BCrypt.Net.BCrypt.HashPassword(model.Haslo);
            // Każdy rejestrujący się użytkownik staje się domyślnie Klientem
            string przydzielonaRola = "Klient"; 

            var newUser = new User 
            { 
                Login = model.Login, 
                Haslo = hashedHaslo,
                Rola = przydzielonaRola,
                KlientId = newKlient.IdKlienta
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // ==========================================================
        // 6. WYLOGOWANIE (GET)
        // ==========================================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}