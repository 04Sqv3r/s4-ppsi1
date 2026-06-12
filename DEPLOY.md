# Wdrożenie aplikacji meow (deployment)

## 1. Lokalnie (Docker)

```bash
docker compose up --build
```

- WWW: http://localhost:5000
- Swagger: http://localhost:5000/swagger

## 2. Produkcja (Docker Compose)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Ustaw zmienne środowiskowe:
- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__Redis`
- `Jwt__Key` (min. 32 znaki)
- `Smtp__Host`, `Smtp__User`, `Smtp__Password` (opcjonalnie)

## 3. Railway (zalecane — darmowy tier)

1. Załóż konto na https://railway.app
2. New Project → Deploy from GitHub repo
3. Dodaj usługi: **MySQL**, **Redis**
4. Ustaw zmienne w aplikacji webowej:
   - `ASPNETCORE_URLS=http://0.0.0.0:8080`
   - `ConnectionStrings__DefaultConnection` = connection string z Railway MySQL
   - `ConnectionStrings__Redis` = URL Redisa z Railway
5. Railway automatycznie wykryje `Dockerfile` i zbuduje obraz
6. Wygeneruj publiczny URL w zakładce Networking

## 4. VPS (Ubuntu + Docker)

```bash
git clone <repo-url>
cd s4-ppsi1-main
docker compose up -d --build
```

Otwórz port 5000 w firewallu lub postaw nginx jako reverse proxy z SSL (Let's Encrypt).

## 5. CI/CD

Pipeline GitHub Actions (`.github/workflows/ci.yml`) uruchamia `dotnet build` przy każdym pushu na `main`.
