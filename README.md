# meow — biblioteka + sklep internetowy

System wypożyczalni książek połączony ze sklepem internetowym (ASP.NET Core MVC + MySQL).

## Uruchomienie (Docker)

```bash
docker compose up --build
```

- Strona WWW: http://localhost:5000
- Swagger API: http://localhost:5000/swagger
- MySQL: port 3307 | Redis: port 6379

## Zagadnienia projektowe (20/20)

| Zagadnienie | Implementacja |
|-------------|---------------|
| MVC | ASP.NET Core MVC |
| Framework CSS | Bootstrap 5 |
| Baza danych | MySQL + EF Core |
| Cache | Redis (`BookCatalogCacheService`) |
| Dependency manager | NuGet |
| HTML / CSS / JS | Widoki Razor + własne style + skrypty |
| Routing | Konwencja `{controller}/{action}/{id?}` |
| ORM | Entity Framework Core |
| Uwierzytelnianie | Sesje + BCrypt + JWT API |
| Lokalizacja | PL/EN — `IStringLocalizer`, przełącznik w menu |
| Mailing | MailKit — potwierdzenie zamówienia (`EmailService`) |
| Formularze | Rejestracja, checkout, panel admina |
| Interakcje async | Live search — `fetch` → `/api/books` |
| Konsumpcja API | Ollama (AI) |
| Publikacja API | REST + Swagger `/swagger` |
| RWD | `responsive.css` + media queries |
| Logger | `ILogger` + `appsettings.json` |
| Deployment | Docker + `DEPLOY.md` + CI GitHub Actions |

## REST API

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/api/books?q=` | Katalog (cache Redis) |
| GET | `/api/books/{id}` | Szczegóły książki |
| POST | `/api/auth/login` | Logowanie JWT |

## SMTP (mailing)

Uzupełnij w `appsettings.json` sekcję `Smtp`. Bez konfiguracji maile logują się w konsoli (`[MAIL-DEV]`).

## Wdrożenie

Instrukcja: [DEPLOY.md](DEPLOY.md)
