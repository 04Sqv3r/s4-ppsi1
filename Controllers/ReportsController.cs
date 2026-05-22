using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using meow.Models;
using System;
using System.Linq;

namespace meow.Controllers
{
    public class ReportsController : Controller
    {
        private readonly LibraryDbContext _context;

        public ReportsController(LibraryDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? data_od, DateTime? data_do)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Account");

            DateTime od = data_od ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime @do = data_do ?? DateTime.Today;

            var raporty = _context.Wypozyczenia
                .Include(w => w.Klient)
                .Include(w => w.Egzemplarz).ThenInclude(e => e!.Book)
                .Include(w => w.Platnosc)
                .Where(w => w.DataWypozyczenia >= od && w.DataWypozyczenia <= @do)
                .OrderByDescending(w => w.DataWypozyczenia)
                .ToList();

            ViewBag.DataOd = od.ToString("yyyy-MM-dd");
            ViewBag.DataDo = @do.ToString("yyyy-MM-dd");
            ViewBag.SumaGlobalna = raporty.Sum(r => r.Platnosc?.Kwota ?? 0m);

            return View(raporty);
        }
    }
}