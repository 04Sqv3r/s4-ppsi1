# Wdrożenie aplikacji meow (deployment)

## Demo produkcyjne

- **WWW:** https://meow-app-production.up.railway.app
- **Swagger:** https://meow-app-production.up.railway.app/swagger

## 1. Lokalnie (Docker)

```bash
docker compose up --build
```

| Usługa | Adres |
|--------|--------|
| WWW | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Mailpit | http://localhost:8025 |

## 2. Produkcja (Docker Compose / VPS)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Zmienne środowiskowe:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__Redis` (opcjonalnie — bez niego cache w pamięci)
- `Jwt__Key` (min. 32 znaki)
- Mailing: patrz [docs/SMTP.md](docs/SMTP.md)

## 3. Railway (zalecane — demo)

### Przez GitHub

1. Konto https://railway.app → New Project → Deploy from GitHub
2. Usługi: **MySQL** (Redis opcjonalny)
3. Variables aplikacji `meow-app`:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
ConnectionStrings__DefaultConnection=<z panelu MySQL>
Jwt__Key=<min. 32 znaki>
Smtp__ApiKey=<xkeysib-... z Brevo>
Smtp__From=<zweryfikowany sender w Brevo>
Smtp__FromName=meow E-Księgarnia
```

4. Networking → Generate Domain (port **8080**)
5. Swagger: `https://<domena>/swagger`

### Przez CLI (bez pusha na GitHub)

```powershell
cd docs/meow-push
.\.tools\railway.exe login
.\.tools\railway.exe up -y --detach --service meow-app
```

Skrypt pomocniczy: [deploy-railway.ps1](deploy-railway.ps1)

## 4. Mailing na Railway

Railway **blokuje port SMTP 587** — używaj **Brevo API**:

1. Brevo → SMTP & API → **API Keys** → `xkeysib-...`
2. Brevo → **Senders** → zweryfikuj adres (`Verified`)
3. Railway → `Smtp__ApiKey` + `Smtp__From`

Szczegóły: [docs/SMTP.md](docs/SMTP.md)

## 5. CI/CD

Pipeline GitHub Actions (`.github/workflows/ci.yml`) — `dotnet build` przy pushu na `main`.

## 6. Pierwsze konto admina

Po rejestracji w aplikacji:

```sql
UPDATE Users SET Rola = 'Admin' WHERE Login = 'twoj_login';
```

Seed przy starcie nadaje admina loginowi `filip` (jeśli istnieje w bazie).
