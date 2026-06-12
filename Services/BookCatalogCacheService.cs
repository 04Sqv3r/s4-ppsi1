using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using meow.Models;
using meow.Models.Api;
using System.Text.Json;

namespace meow.Services
{
    public class BookCatalogCacheService
    {
        private const string CatalogKey = "meow:catalog:all";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private readonly LibraryDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<BookCatalogCacheService> _logger;

        public BookCatalogCacheService(
            LibraryDbContext context,
            IDistributedCache cache,
            ILogger<BookCatalogCacheService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IReadOnlyList<BookDto>> GetCatalogAsync(CancellationToken ct = default)
        {
            var cached = await _cache.GetStringAsync(CatalogKey, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("Katalog książek odczytany z cache (Redis).");
                return JsonSerializer.Deserialize<List<BookDto>>(cached) ?? [];
            }

            _logger.LogInformation("Cache miss — ładuję katalog z bazy MySQL.");
            var books = await LoadFromDatabaseAsync(ct);

            await _cache.SetStringAsync(
                CatalogKey,
                JsonSerializer.Serialize(books),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                ct);

            return books;
        }

        public async Task<BookDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var catalog = await GetCatalogAsync(ct);
            return catalog.FirstOrDefault(b => b.Id == id);
        }

        public async Task InvalidateAsync(CancellationToken ct = default)
        {
            await _cache.RemoveAsync(CatalogKey, ct);
            _logger.LogInformation("Unieważniono cache katalogu książek.");
        }

        private async Task<List<BookDto>> LoadFromDatabaseAsync(CancellationToken ct)
        {
            var wypozyczoneIds = await _context.Wypozyczenia
                .Where(w => w.DataZwrotu == null && w.IdEgzemplarz != null)
                .Select(w => w.IdEgzemplarz!.Value)
                .ToListAsync(ct);

            var wolnePerBook = await _context.Egzemplarze
                .Where(e => !wypozyczoneIds.Contains(e.IdEgzemplarza))
                .GroupBy(e => e.IdKsiazka)
                .Select(g => new { BookId = g.Key, Wolne = g.Count() })
                .ToDictionaryAsync(x => x.BookId, x => x.Wolne, ct);

            var books = await _context.Books.OrderBy(b => b.Id).ToListAsync(ct);

            return books.Select(b => new BookDto
            {
                Id = b.Id,
                Tytul = b.Tytul,
                Autor = b.Autor,
                Gatunek = b.Gatunek,
                RokWydania = b.RokWydania,
                Cena = b.Cena,
                IloscDoSprzedazy = b.IloscDoSprzedazy,
                IloscEgzemplarzy = b.IloscEgzemplarzy,
                Opis = b.Opis,
                ImageUrl = b.ImageUrl,
                DostepneDoWypozyczenia = wolnePerBook.GetValueOrDefault(b.Id, 0)
            }).ToList();
        }
    }
}
