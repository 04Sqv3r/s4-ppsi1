using meow.Models;
using Microsoft.EntityFrameworkCore;

namespace meow.Services
{
    public static class DatabaseSeeder
    {
        public static readonly string[] AllGenres =
        {
            "Biografia", "Biznes", "Ezoteryka i parapsychologia", "Fantasy", "Historia",
            "Komiksy, Mangy", "Kryminał", "Dla dzieci", "Dla młodzieży", "Kuchnia i diety",
            "Popularnonaukowa", "Literatura obcojęzyczna", "Literatura obyczajowa", "Powieść",
            "Nauka języków", "Nauki humanistyczne", "Naukowe", "Podręczniki akademickie",
            "Podróże i turystyka", "Poezja", "Poradniki", "Prawo", "Religia", "Sport",
            "Wiek-0-2", "Wiek-3-5", "Wiek-6-8", "Wiek-9-12", "Emocje", "Kariera", "Psychologia"
        };

        public static async Task SeedAsync(LibraryDbContext context, ILogger logger, CancellationToken ct = default)
        {
            var catalog = BuildCatalog();
            var existingGenres = await context.Books.Select(b => b.Gatunek).Distinct().ToListAsync(ct);

            var toAdd = catalog.Where(b => !existingGenres.Contains(b.Gatunek)).ToList();
            if (toAdd.Count > 0)
            {
                logger.LogInformation("Seed: dodaję {Count} książek (brakujące kategorie)…", toAdd.Count);
                context.Books.AddRange(toAdd);
                await context.SaveChangesAsync(ct);
                await AddCopiesAsync(context, toAdd, ct);
            }

            var coversUpdated = await SyncCoverUrlsAsync(context, catalog, ct);
            if (coversUpdated > 0)
                logger.LogInformation("Seed: zaktualizowano {Count} okładek.", coversUpdated);

            await EnsureAdminAsync(context, logger, ct);
            await FixLegacyRentalDueDatesAsync(context, logger, ct);
        }

        /// <summary>
        /// Aktywne wypożyczenia utworzone ze starym terminem 14 dni — przeliczenie na 30.
        /// </summary>
        private static async Task FixLegacyRentalDueDatesAsync(
            LibraryDbContext context, ILogger logger, CancellationToken ct)
        {
            var active = await context.Wypozyczenia
                .Where(w => w.DataZwrotu == null)
                .ToListAsync(ct);

            var fixedCount = 0;
            foreach (var w in active)
            {
                var issueDate = w.DataWypozyczenia.Date;
                var due = w.DataPlanowanegoZwrotu.Date;
                var legacyDue = issueDate.AddDays(LibraryConstants.LegacyLoanPeriodDays);
                var pickupDue = issueDate.AddDays(LibraryConstants.ReservationPickupDays);

                if (due == legacyDue && due != pickupDue)
                {
                    w.DataPlanowanegoZwrotu = issueDate.AddDays(LibraryConstants.LoanPeriodDays);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Seed: poprawiono {Count} terminów zwrotu (14→30 dni).", fixedCount);
            }
        }

        private static async Task<int> SyncCoverUrlsAsync(
            LibraryDbContext context, List<Book> catalog, CancellationToken ct)
        {
            var byTitle = catalog.ToDictionary(b => b.Tytul, b => b.ImageUrl!);
            var books = await context.Books.ToListAsync(ct);
            var updated = 0;

            foreach (var book in books)
            {
                var url = byTitle.GetValueOrDefault(book.Tytul)
                    ?? catalog.FirstOrDefault(c => book.Tytul.StartsWith(c.Tytul, StringComparison.OrdinalIgnoreCase)
                        || c.Tytul.StartsWith(book.Tytul, StringComparison.OrdinalIgnoreCase))?.ImageUrl
                    ?? Pl(ShortLabel(book.Tytul), "234465");

                if (book.ImageUrl != url)
                {
                    book.ImageUrl = url;
                    updated++;
                }
            }

            if (updated > 0)
                await context.SaveChangesAsync(ct);

            return updated;
        }

        private static string ShortLabel(string tytul)
        {
            var words = tytul.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length <= 3 ? tytul : string.Join(' ', words.Take(3));
        }

        private static async Task AddCopiesAsync(LibraryDbContext context, List<Book> books, CancellationToken ct)
        {
            var stany = new[] { "idealny", "idealny", "dobry", "dobry", "używany" };
            var egzemplarze = new List<Egzemplarz>();

            foreach (var book in books)
            {
                for (var i = 0; i < book.IloscEgzemplarzy; i++)
                {
                    egzemplarze.Add(new Egzemplarz
                    {
                        IdKsiazka = book.Id,
                        NumerInwentarzowy = $"INV-2026-{book.Id:D4}-{i + 1:D2}",
                        Stan = stany[i % stany.Length]
                    });
                }
            }

            context.Egzemplarze.AddRange(egzemplarze);
            await context.SaveChangesAsync(ct);
        }

        private static async Task EnsureAdminAsync(LibraryDbContext context, ILogger logger, CancellationToken ct)
        {
            var filip = await context.Users.FirstOrDefaultAsync(u => u.Login == "filip", ct);
            if (filip != null && filip.Rola != "Admin")
            {
                filip.Rola = "Admin";
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Seed: użytkownik 'filip' → Admin.");
            }
        }

        private static List<Book> BuildCatalog()
        {
            var items = new List<Book>
            {
            B("Steve Jobs", "Walter Isaacson", "Biografia", 2011, 54.99m, 10, 2,
                "Autoryzowana biografia współzałożyciela Apple.", "9781451648539", "Simon & Schuster", 656,
                OlIsbn("9781451648539")),
            B("Empati. Przewodnik po uczuciach w biznesie", "Jacek Santorski", "Biznes", 2016, 48.99m, 12, 2,
                "Emocje w pracy zespołowej i zarządzaniu.", "9788328034567", "Słowo/obraz terytoria", 320,
                Pl("Empati", "1e3a5f")),
            B("Tarot. Podręcznik dla początkujących", "Kathleen McElroy", "Ezoteryka i parapsychologia", 2018, 39.99m, 8, 2,
                "78 kart, rozkłady i symbolika.", "9780738759848", "Galaktyka", 192,
                OlIsbn("9780738759848")),
            B("Wiedźmin: Ostatnie życzenie", "Andrzej Sapkowski", "Fantasy", 1993, 49.99m, 18, 5,
                "Geralt z Rivii — polska klasyka fantasy.", "9788375780565", "SuperNowa", 288,
                OlIsbn("9780316383616")),
            B("Epoka lodowcowa. Europa 1450–1550", "Norman Davies", "Historia", 2022, 69.99m, 7, 3,
                "Historia Europy wczesnowspółczesnej.", "9780231128179", "Znak Horyzont", 512,
                OlIsbn("9780198201713")),
            B("Watchmen", "Alan Moore", "Komiksy, Mangy", 1987, 89.99m, 9, 2,
                "Kultowy komiks o superbohaterach.", "9780930289232", "DC Comics", 416,
                OlIsbn("9780930289232")),
            B("Biały Jar", "Rafał Kosik", "Kryminał", 2019, 43.99m, 14, 4,
                "Detektyw Marcin Maj w Beskidach.", "9788381789012", "PowerGraph", 368,
                Pl("Bialy Jar", "0f172a")),
            B("Mały Książę", "Antoine de Saint-Exupéry", "Dla dzieci", 1943, 29.99m, 22, 6,
                "Opowieść o przyjaźni i różach.", "9780156012195", "Znak", 96,
                OlIsbn("9780156012195")),
            B("Czarne oczy", "Katarzyna Berenika Miszczuk", "Dla młodzieży", 2014, 35.99m, 16, 4,
                "Urban fantasy w polskiej mitologii.", "9788389012345", "Jaguar", 384,
                Pl("Czarne oczy", "4c1d95")),
            B("Kuchnia polska. Tradycja z nowoczesnym smakiem", "Magda Gessler", "Kuchnia i diety", 2018, 64.99m, 11, 1,
                "Pierogi, bigos, żurek i więcej.", "9788381234789", "Znak Horyzont", 312,
                Pl("Kuchnia PL", "c2410c")),
            B("Sapiens. Od zwierząt do bogów", "Yuval Noah Harari", "Popularnonaukowa", 2014, 59.99m, 20, 3,
                "Krótka historia ludzkości.", "9780062316097", "WL", 544,
                OlIsbn("9780062316097")),
            B("The Great Gatsby", "F. Scott Fitzgerald", "Literatura obcojęzyczna", 1925, 32.99m, 15, 4,
                "Jazz Age i amerykański sen.", "9780743273565", "Scribner", 180,
                OlIsbn("9780743273565")),
            B("Dziennik", "Witold Gombrowicz", "Literatura obyczajowa", 1953, 37.99m, 9, 3,
                "Gombrowicz w Argentynie.", "9788308015678", "WL", 384,
                Pl("Dziennik", "334155")),
            B("Lalka", "Bolesław Prus", "Powieść", 1890, 44.99m, 16, 5,
                "Wokulski i Warszawa XIX wieku.", "9788377321234", "Znak", 752,
                Pl("Lalka", "234465")),
            B("English Grammar in Use", "Raymond Murphy", "Nauka języków", 2019, 89.99m, 13, 2,
                "Podręcznik gramatyki angielskiej.", "9781108457651", "Cambridge", 392,
                OlIsbn("9781108457651")),
            B("Krótkie historie filozofii", "Nigel Warburton", "Nauki humanistyczne", 2011, 46.99m, 8, 2,
                "Od Sokratesa po współczesność.", "9780198742776", "OUP", 272,
                OlIsbn("9780198742776")),
            B("Atlas anatomii człowieka", "Frank H. Netter", "Naukowe", 2014, 199.99m, 6, 1,
                "Atlas anatomii dla studentów medycyny.", "9780323393225", "Elsevier", 640,
                OlIsbn("9780323393225")),
            B("Mikroekonomia", "N. Gregory Mankiw", "Podręczniki akademickie", 2020, 119.99m, 8, 2,
                "Podręcznik z mikroekonomii.", "9780324314168", "PWE", 560,
                OlIsbn("9780324314168")),
            B("Lonely Planet. Islandia", "Lonely Planet", "Podróże i turystyka", 2023, 79.99m, 10, 1,
                "Przewodnik po Islandii.", "9781788680418", "Lonely Planet", 384,
                OlIsbn("9781788680418")),
            B("Wiersze wybrane", "Wisława Szymborska", "Poezja", 2004, 34.99m, 11, 3,
                "Wiersze noblistki.", "9788308044567", "WL", 416,
                Pl("Szymborska", "475569")),
            B("Jak zdobyć przyjaciół i wpływać na ludzi", "Dale Carnegie", "Poradniki", 1936, 36.99m, 14, 2,
                "Klasyka komunikacji interpersonalnej.", "9780671027032", "Rebis", 384,
                OlIsbn("9780671027032")),
            B("Kodeks cywilny. Komentarz", "Janusz Barta", "Prawo", 2023, 149.99m, 5, 1,
                "Komentarz do Kodeksu cywilnego.", "9788326912345", "C.H. Beck", 1200,
                Pl("Kodeks cyw.", "1e293b")),
            B("Biblia. Tysiąclecie", "Pallottinum", "Religia", 2015, 89.99m, 12, 2,
                "Pismo Święte — przekład polski.", "9788375160690", "Pallottinum", 1632,
                Pl("Biblia", "78350f")),
            B("Pep Guardiola. Moja wizja futbolu", "Pep Guardiola", "Sport", 2016, 44.99m, 10, 2,
                "Filozofia gry Guardioli.", "9780224091677", "WDoŚ", 320,
                OlIsbn("9780224091677")),
            B("Pucio. Czego dotykam?", "Martyna Galewska", "Wiek-0-2", 2019, 19.99m, 25, 5,
                "Książeczka sensoryczna dla maluchów.", "9788383212340", "Wilga", 12,
                Pl("Pucio 0-2", "f59e0b")),
            B("Pucio. Idzie do dentysty", "Martyna Galewska", "Wiek-3-5", 2020, 24.99m, 20, 6,
                "Pucio idzie do dentysty.", "9788383215678", "Wilga", 24,
                Pl("Pucio 3-5", "f59e0b")),
            B("Karolcia", "Maria Krüger", "Wiek-6-8", 1959, 28.99m, 15, 5,
                "Karolcia i kocur Felek.", "9788375612345", "Nasza Księgarnia", 208,
                Pl("Karolcia", "059669")),
            B("Harry Potter i Kamień Filozoficzny", "J.K. Rowling", "Wiek-9-12", 1997, 54.99m, 22, 5,
                "Harry trafia do Hogwartu.", "9780439139601", "Media Rodzina", 336,
                OlIsbn("9780439139601")),
            B("Wielkie uczucia. Smutek", "Katarzyna Malkowska", "Emocje", 2021, 26.99m, 18, 4,
                "Pomaga dzieciom zrozumieć smutek.", "9788383217890", "Egmont", 32,
                Pl("Smutek", "6366f1")),
            B("Mentalność zwycięzcy", "Carol S. Dweck", "Kariera", 2017, 42.99m, 13, 2,
                "Growth mindset a rozwój kariery.", "9780345472328", "Galaktyka", 320,
                OlIsbn("9780345472328")),
            B("Psychologia pieniędzy", "Morgan Housel", "Psychologia", 2020, 52.99m, 16, 3,
                "Decyzje finansowe i zachowania.", "9780857197689", "MT Biznes", 256,
                OlIsbn("9780857197689")),

            B("Hobbit, czyli tam i z powrotem", "J.R.R. Tolkien", "Fantasy", 1937, 47.99m, 14, 4,
                "Bilbo i Smaug.", "9780547928227", "Amber", 368,
                OlIsbn("9780547928227")),
            B("1984", "George Orwell", "Powieść", 1949, 34.99m, 18, 5,
                "Antyutopia Wielkiego Brata.", "9780451524935", "Muza", 416,
                OlIsbn("9780451524935")),
            B("Zły", "Leopold Tyrmand", "Kryminał", 1955, 39.99m, 10, 3,
                "Mroczna Warszawa lat 50.", "9788308047336", "W.A.B.", 520,
                Pl("Zly", "0f172a")),
            B("Solaris", "Stanisław Lem", "Fantasy", 1961, 42.99m, 11, 3,
                "Ocean Solaris i świadomość.", "9780156027606", "WL", 464,
                OlIsbn("9780156027606")),
            B("Dune: Diuna", "Frank Herbert", "Fantasy", 1965, 56.99m, 13, 3,
                "Paul Atryda na Arrakis.", "9780441172719", "Rebis", 688,
                OlIsbn("9780441172719")),
            B("W pustyni i w puszczy", "Henryk Sienkiewicz", "Dla dzieci", 1911, 32.99m, 14, 4,
                "Staś i Nel w Afryce.", "9788373015678", "Greg", 448,
                Pl("W pustyni", "b45309")),
            B("Król", "Szczepan Twardoch", "Powieść", 2019, 41.99m, 10, 3,
                "Polska lat 90. i futbol.", "9788381234567", "WL", 424,
                Pl("Krol", "234465"))
            };
            return items.GroupBy(b => b.Tytul).Select(g => g.First()).ToList();
        }

        private static Book B(
            string tytul, string autor, string gatunek, int rok, decimal cena,
            int sklep, int biblioteka, string opis, string isbn, string wydawnictwo, int strony,
            string coverUrl) =>
            new()
            {
                Tytul = tytul,
                Autor = autor,
                Gatunek = gatunek,
                RokWydania = rok,
                Cena = cena,
                CenaOkladkowa = Math.Round(cena * 1.15m, 2),
                IloscDoSprzedazy = sklep,
                IloscEgzemplarzy = biblioteka,
                Opis = opis,
                EAN = isbn,
                Indeks = $"MEOW-{isbn[^4..]}",
                Wydawnictwo = wydawnictwo,
                LiczbaStron = strony,
                OkladkaTyp = "miękka",
                JezykWydania = gatunek == "Literatura obcojęzyczna" ? "angielski" : "polski",
                NumerWydania = "I",
                DataWydania = new DateTime(rok, 6, 1),
                ImageUrl = coverUrl,
                WysokoscMm = 210,
                SzerokoscMm = 145,
                GlebokoscMm = 25
            };

        /// <summary>Open Library — działające ISBN-y wydawnictw zagranicznych (okładka ta sama lub podobna).</summary>
        private static string OlIsbn(string isbn) =>
            $"https://covers.openlibrary.org/b/isbn/{isbn}-M.jpg?default=false";

        /// <summary>Estetyczna okładka zastępcza dla polskich tytułów bez skanu w Open Library.</summary>
        private static string Pl(string label, string colorHex) =>
            $"https://placehold.co/220x320/{colorHex}/ffffff?font=source-sans-pro&text={Uri.EscapeDataString(label)}";
    }
}
