using Microsoft.AspNetCore.Mvc;
using meow.Models.Api;
using meow.Services;

namespace meow.Controllers.Api
{
    [ApiController]
    [Route("api/books")]
    [Produces("application/json")]
    public class BooksApiController : ControllerBase
    {
        private readonly BookCatalogCacheService _catalog;
        private readonly ILogger<BooksApiController> _logger;

        public BooksApiController(BookCatalogCacheService catalog, ILogger<BooksApiController> logger)
        {
            _catalog = catalog;
            _logger = logger;
        }

        /// <summary>Lista wszystkich książek (z cache Redis).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAll(
            [FromQuery] string? gatunek,
            [FromQuery] string? q,
            CancellationToken ct)
        {
            var books = await _catalog.GetCatalogAsync(ct);

            if (!string.IsNullOrWhiteSpace(gatunek))
                books = books.Where(b => b.Gatunek == gatunek).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                books = books
                    .Where(b => b.Tytul.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || b.Autor.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogInformation("API GET /api/books — zwrócono {Count} pozycji.", books.Count);
            return Ok(books);
        }

        /// <summary>Szczegóły pojedynczej książki.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookDto>> GetById(int id, CancellationToken ct)
        {
            var book = await _catalog.GetByIdAsync(id, ct);
            if (book == null)
            {
                _logger.LogWarning("API GET /api/books/{Id} — nie znaleziono.", id);
                return NotFound(new { message = $"Książka o id={id} nie istnieje." });
            }

            return Ok(book);
        }

        /// <summary>Wymusza odświeżenie cache katalogu (admin / dev).</summary>
        [HttpPost("refresh-cache")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RefreshCache(CancellationToken ct)
        {
            await _catalog.InvalidateAsync(ct);
            _logger.LogInformation("API POST /api/books/refresh-cache — cache wyczyszczony.");
            return NoContent();
        }
    }
}
