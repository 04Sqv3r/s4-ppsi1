# meow — biblioteka + sklep internetowy

System wypożyczalni książek połączony ze sklepem internetowym (ASP.NET Core MVC + MySQL + REST API).

**Repozytorium:** https://github.com/04Sqv3r/s4-ppsi1  
**Gałąź robocza:** `filip/aktualna-wersja`

## Demo produkcyjne (Railway)

| Usługa | URL |
|--------|-----|
| Aplikacja WWW | https://meow-app-production.up.railway.app |
| Swagger / OpenAPI | https://meow-app-production.up.railway.app/swagger |
| OpenAPI JSON | https://meow-app-production.up.railway.app/swagger/v1/swagger.json |

## Uruchomienie lokalne (Docker)

```bash
docker compose up --build
```

| Usługa | Adres |
|--------|--------|
| Strona WWW | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| MySQL | port 3307 |
| Redis | port 6379 |
| Mailpit (podgląd maili) | http://localhost:8025 |

## Zagadnienia projektowe

| Zagadnienie | Status | Implementacja |
|-------------|--------|---------------|
| MVC | ✅ | ASP.NET Core MVC |
| Framework CSS | ✅ | Bootstrap 5 |
| Baza danych | ✅ | MySQL + EF Core |
| Cache | ✅ | Redis (opcjonalnie) / fallback pamięć |
| Dependency manager | ✅ | NuGet |
| HTML / CSS / JS | ✅ | Widoki Razor + własne style + skrypty |
| Routing | ✅ | Konwencja `{controller}/{action}/{id?}` |
| ORM | ✅ | Entity Framework Core |
| Uwierzytelnianie | ✅ | Sesje + BCrypt + JWT API |
| Lokalizacja | ✅ | PL/EN — UI + panel admina |
| Mailing | ✅ | MailKit + Brevo API (prod) / Mailpit (Docker) — [docs/SMTP.md](docs/SMTP.md) |
| Formularze | ✅ | Rejestracja, checkout, panel admina |
| Interakcje async | ✅ | Live search — `fetch` → `/api/books` |
| Konsumpcja API | 🟡 | Szkic `AIChat` / Ollama (opcjonalnie) |
| Publikacja API | ✅ | REST + Swagger `/swagger` |
| RWD | ✅ | `responsive.css` + media queries |
| Logger | ✅ | `ILogger` + `appsettings.json` |
| Deployment | ✅ | Docker + Railway |

## REST API (Swagger)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/api/books?q=&gatunek=` | Katalog (cache) |
| GET | `/api/books/{id}` | Szczegóły książki |
| POST | `/api/auth/login` | Logowanie JWT |
| POST | `/api/books/refresh-cache` | Unieważnienie cache katalogu |

Pełna dokumentacja interaktywna: `/swagger`.

## SMTP (mailing)

Instrukcja: [docs/SMTP.md](docs/SMTP.md)

- **Docker:** Mailpit — http://localhost:8025
- **Railway:** `Smtp__ApiKey` (Brevo API) + zweryfikowany `Smtp__From`
- **Lokalnie:** `appsettings.Smtp.local.json` (szablon: `appsettings.Smtp.example.json`)

## Wdrożenie

- [DEPLOY.md](DEPLOY.md) — Docker, Railway, VPS
- [docs/SMTP.md](docs/SMTP.md) — konfiguracja maili
- [docs/README.md](docs/README.md) — sprawozdanie PDF (LaTeX)

## Zespół

| Osoba | Obszar |
|-------|--------|
| Filip Jędryczkowski | MVC, lokalizacja, Railway, mailing, checkout, raporty |
| Kyrylo Shchedrin | Integracja AI, repozytorium GitHub |
| Nikola Pisarska | Współpraca przy projekcie PPSI I |

## Dokumentacja projektowa

Sprawozdanie LaTeX: [docs/sprawozdanie.tex](docs/sprawozdanie.tex) — kompilacja opisana w [docs/README.md](docs/README.md).
