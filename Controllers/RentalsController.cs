using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using meow.Models;
using meow.Resources;
using meow.Services;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace meow.Controllers
{
    [MeowAuthorize("Admin")]
    public class RentalsController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public RentalsController(LibraryDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // ==========================================================
        // 1. FORMULARZ METODY GET (DODAWANIE WYPOŻYCZENIA PRZEZ ADMINA)
        // ==========================================================
        [HttpGet]
        public IActionResult Create(int? bookId)
        {
            // Zabezpieczenie roli przed nieautoryzowanym dostępem
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            ViewBag.Klienci = _context.Klienci.OrderBy(k => k.Nazwisko).ToList();

            var wypozyczoneEgzemplarzeIds = _context.Wypozyczenia
                .Where(w => w.DataZwrotu == null && w.IdEgzemplarz != null)
                .Select(w => w.IdEgzemplarz)
                .ToList();

            var dostepneQuery = _context.Egzemplarze
                .Include(e => e.Book)
                .Where(e => !wypozyczoneEgzemplarzeIds.Contains(e.IdEgzemplarza));

            if (bookId.HasValue)
            {
                var wybraneId = bookId.Value;
                ViewBag.SelectedBookId = wybraneId;
            }

            ViewBag.DostepneEgzemplarze = dostepneQuery.ToList();

            return View();
        }

        // ==========================================================
        // 2. ZAPIS FORMULARZA POST (MANUALNA REJESTRACJA WYDANIA - ADMIN)
        // ==========================================================
        [HttpPost]
        [MeowAuthorize]
        public IActionResult Create(int id_klient, int id_egzemplarz, DateTime data_wypozyczenia)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            if (data_wypozyczenia > DateTime.Today)
            {
                TempData["Message"] = _localizer["Msg_RentalDateFuture"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Create");
            }

            DateTime dataPlanowana = data_wypozyczenia.AddDays(LibraryConstants.LoanPeriodDays);

            var wypozyczenie = new Wypozyczenie
            {
                IdKlient = id_klient,
                IdEgzemplarz = id_egzemplarz,
                DataWypozyczenia = data_wypozyczenia,
                DataPlanowanegoZwrotu = dataPlanowana,
                DataZwrotu = null
            };

            _context.Wypozyczenia.Add(wypozyczenie);
            _context.SaveChanges();

            TempData["Message"] = _localizer["Msg_RentalRegistered"].Value;
            TempData["MessageType"] = "success";
            return RedirectToAction("Returns");
        }

      // ==========================================================
        // 3. SPIS AKTYWNYCH WYPOŻYCZEŃ BIBLIOTECZNYCH I HISTORIA ZAMÓWIEŃ SKLEPU
        // ==========================================================
        [HttpGet]
        public IActionResult Returns()
        {
            // Pełna kontrola dostępu oparta na roli
            if (HttpContext.Session.GetString("UserRole") != "Admin") 
                return RedirectToAction("Login", "Account");

            // 1. Pobieramy aktywne, fizyczne wypożyczenia biblioteczne (Nienaruszone)
            var listaWypozyczen = _context.Wypozyczenia
                .Include(w => w.Klient)
                .Include(w => w.Egzemplarz).ThenInclude(e => e!.Book)
                .Where(w => w.DataZwrotu == null && w.IdEgzemplarz != null)
                .OrderByDescending(w => w.DataWypozyczenia)
                .ToList();

            // 2. Pobieramy WSZYSTKIE zamówienia ze sklepu internetowego z bazy danych
            var suroweZamowieniaSklepowe = _context.Zamowienia
                .Include(z => z.Klient)
                .Include(z => z.Book)
                .ToList();

            // 3. Budujemy dla Admina zaawansowaną strukturę historii zamówień (Grupowanie po numerze paczki)
            var historiaZamowienDlaAdmina = suroweZamowieniaSklepowe
                .GroupBy(z => z.NumerSledzenia ?? Guid.NewGuid().ToString())
                .Select(g => {
                    var pierwsze = g.First();
                    
                    // Zliczamy unikalne pozycje i ich ilości w tym konkretnym zamówieniu
                    var pozycjeWZamowieniu = g
                        .Where(z => z.Book != null)
                        .GroupBy(z => z.IdKsiazki)
                        .Select(bGroup => new {
                            Tytul = bGroup.First().Book!.Tytul,
                            Autor = bGroup.First().Book!.Autor,
                            Cena = bGroup.First().Book!.Cena ?? 0.00m,
                            Ilosc = bGroup.Count()
                        }).ToList();

                    // Obliczamy łączną wartość finansową całego koszyka klienta
                    decimal lacznaWartosc = pozycjeWZamowieniu.Sum(p => p.Cena * p.Ilosc);

                    // Tworzymy czytelny opis tekstowy zakupionych pozycji dla tabeli głównej Admina
                    string skrótZakupów = string.Join(", ", pozycjeWZamowieniu.Select(p => $"„{p.Tytul}” ({p.Ilosc} szt.)"));

                    // Używamy tymczasowego obiektu anonimowego przesyłanego przez ViewBag
                    return new {
                        OrderId = pierwsze.Id,
                        KlientNazwa = pierwsze.Klient != null ? $"{pierwsze.Klient.Imie} {pierwsze.Klient.Nazwisko}" : "Klient Sklepowy",
                        KlientEmail = pierwsze.Klient?.Email ?? "-",
                        KlientTelefon = pierwsze.Klient?.Telefon ?? "-",
                        DataZlozenia = pierwsze.DataZamowienia.ToString("yyyy-MM-dd HH:mm"),
                        Status = pierwsze.Status,
                        TrackingNumber = pierwsze.NumerSledzenia ?? "Brak numeru",
                        WartoscZamowienia = lacznaWartosc,
                        OpisPozycji = skrótZakupów,
                        SzczegolyPozycji = pozycjeWZamowieniu // Przekazujemy pełną listę do rozwijanego menu panelu
                    };
                })
                .OrderByDescending(z => z.OrderId)
                .ToList<object>(); // Rzutujemy na listę obiektów, aby ViewBag bez problemu ją przetworzył

            // Przekazujemy gotową strukturę historii do widoku panelu administracyjnego
            ViewBag.ZamowieniaSklepowe = historiaZamowienDlaAdmina;

            return View(listaWypozyczen);
        }
        // ==========================================================
        // 4. POTWIERDZENIE ODBIORU REZERWACJI (START LICZENIA 30 DNI)
        // ==========================================================
        [HttpPost]
        public IActionResult ZatwierdzOdbior(int id_wypozyczenie)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var wypozyczenie = _context.Wypozyczenia.Find(id_wypozyczenie);
            if (wypozyczenie == null) return RedirectToAction("Returns");

            wypozyczenie.DataWypozyczenia = DateTime.Today;
            wypozyczenie.DataPlanowanegoZwrotu = DateTime.Today.AddDays(LibraryConstants.LoanPeriodDays);

            _context.SaveChanges();

            TempData["Message"] = _localizer["Msg_BookIssued"].Value;
            TempData["MessageType"] = "success";

            return RedirectToAction("Returns");
        }

        // ==========================================================
        // 5. REZERWACJA ONLINE PRZEZ KLIENTA (Z WYBOREM EGZEMPLARZA)
        // ==========================================================
        [HttpPost]
        public IActionResult Zarezerwuj(int idEgzemplarza)
        {
            // 1. Najpierw pobieramy dane o wybranym egzemplarzu, żeby znać ID książki
            var egzemplarz = _context.Egzemplarze
                .Include(e => e.Book)
                .FirstOrDefault(e => e.IdEgzemplarza == idEgzemplarza);

            if (egzemplarz == null)
            {
                TempData["Message"] = _localizer["Msg_CopyNotFound"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "Shop");
            }

            int idKsiazki = egzemplarz.Book?.Id ?? 1;

            // 2. Sprawdzamy, czy użytkownik w ogóle jest zalogowany
            if (HttpContext.Session.GetString("User") == null)
            {
                TempData["Message"] = _localizer["Msg_ReserveLogin"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Login", "Account", new { returnUrl = $"/Shop/Details/{idKsiazki}" });
            }

            // 3. Bezpiecznie wyciągamy IdKlienta z sesji
            int? idKlienta = HttpContext.Session.GetInt32("UserId");
            if (idKlienta == null)
            {
                var domyslnyKlient = _context.Klienci.FirstOrDefault();
                idKlienta = domyslnyKlient?.IdKlienta ?? 1;
            }

            // 4. NAPRAWIONA KOLEJNOŚĆ: Teraz sprawdzamy blokadę przetrzymania, bo znamy już idKlienta i idKsiazki!
            bool maZaleglosci = _context.Wypozyczenia.Any(w => 
                w.IdKlient == idKlienta.Value && 
                w.DataZwrotu == null && 
                DateTime.Now > w.DataPlanowanegoZwrotu);

            if (maZaleglosci)
            {
                TempData["Message"] = _localizer["Msg_ReserveBlocked"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Details", "Shop", new { id = idKsiazki });
            }

            // 5. Sprawdzamy, czy ktoś nas nie ubiegł z tą rezerwacją
            var czyZajety = _context.Wypozyczenia.Any(w => w.IdEgzemplarz == idEgzemplarza && w.DataZwrotu == null);
            if (czyZajety)
            {
                TempData["Message"] = _localizer["Msg_ReserveTaken"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Details", "Shop", new { id = idKsiazki });
            }

            DateTime dataNaOdbior = DateTime.Today.AddDays(LibraryConstants.ReservationPickupDays);

            var nowaRezerwacja = new Wypozyczenie
            {
                IdKlient = idKlienta.Value,
                IdEgzemplarz = idEgzemplarza,
                IdKsiazki = idKsiazki, // Trwałe przypisanie ID książki dla widoku profilu
                DataWypozyczenia = DateTime.Today,
                DataPlanowanegoZwrotu = dataNaOdbior, 
                DataZwrotu = null
            };

            _context.Wypozyczenia.Add(nowaRezerwacja);
            _context.SaveChanges();

            TempData["Message"] = string.Format(_localizer["Msg_ReserveSuccess"].Value, egzemplarz.NumerInwentarzowy, dataNaOdbior.ToString("dd.MM.yyyy"));
            TempData["MessageType"] = "success";

            return RedirectToAction("Details", "Shop", new { id = idKsiazki });
        }

        // ==========================================================
        // 6. OBSŁUGA ZWROTÓW (URZĘDNIK) + NALICZANIE KAR
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ZwrocKsiazke(int id_wypozyczenie, DateTime data_zwrotu)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            var wypozyczenie = _context.Wypozyczenia.Find(id_wypozyczenie);
            if (wypozyczenie == null)
                return RedirectToAction("Returns");

            if (data_zwrotu < wypozyczenie.DataWypozyczenia)
            {
                TempData["Message"] = _localizer["Msg_ReturnDateInvalid"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Returns");
            }

            wypozyczenie.DataZwrotu = data_zwrotu;

            if (data_zwrotu.Date > wypozyczenie.DataPlanowanegoZwrotu.Date)
            {
                int dniSpoznienia = (data_zwrotu.Date - wypozyczenie.DataPlanowanegoZwrotu.Date).Days;
                decimal kara = dniSpoznienia * 0.50m;

                var platnosc = new Platnosc
                {
                    IdWypozyczenie = id_wypozyczenie,
                    Kwota = kara
                };

                _context.Platnosci.Add(platnosc);
                _context.SaveChanges();

                TempData["Message"] = string.Format(_localizer["Msg_ReturnFine"].Value, dniSpoznienia, kara.ToString("F2"));
                TempData["MessageType"] = "error";
            }
            else
            {
                _context.SaveChanges();
                TempData["Message"] = _localizer["Msg_ReturnOnTime"].Value;
                TempData["MessageType"] = "success";
            }

            return RedirectToAction("Returns");
        }

        // ==========================================================
        // 7. ZWROT REKOMENDOWANY DLA LINKÓW ZEWNĘTRZNYCH (ZABEZPIECZENIE)
        // ==========================================================
        [HttpGet]
        public IActionResult Orders()
        {
            return RedirectToAction("Returns");
        }

        // ==========================================================
// 8. ZMIANA STATUSU: ZATWIERDZENIE WYSYŁKI PACZKI SKLEPOWEJ
// ==========================================================
        [HttpPost]
        public IActionResult ZatwierdzWysylke(string trackingNumber)
        {
            // Pełna ochrona roli
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(trackingNumber))
            {
                TempData["Message"] = _localizer["Msg_TrackingMissing"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Returns");
            }

            // POPRAWKA: Pobieramy od razu wszystkie pozycje z tym konkretnym numerem śledzenia
            var calaPaczka = _context.Zamowienia
                .Where(z => z.NumerSledzenia == trackingNumber)
                .ToList();

            if (!calaPaczka.Any())
            {
                TempData["Message"] = _localizer["Msg_PackageNotFound"].Value;
                TempData["MessageType"] = "error";
                return RedirectToAction("Returns");
            }

            // Zmieniamy status wszystkim elementom wchodzącym w skład tej przesyłki
            foreach (var item in calaPaczka)
            {
                item.Status = "Wysłana";
            }

            _context.SaveChanges();

            TempData["Message"] = string.Format(_localizer["Msg_PackageShipped"].Value, trackingNumber);
            TempData["MessageType"] = "success";

            return RedirectToAction("Returns");
        }
    }
}