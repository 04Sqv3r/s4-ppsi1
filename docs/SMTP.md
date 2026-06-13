# Konfiguracja SMTP (mailing)

Aplikacja wysyła maile po złożeniu zamówienia w sklepie (`EmailService` + MailKit).

## Tryb deweloperski (Docker)

Przy `docker compose up` działa **Mailpit** — przechwytuje maile bez wysyłki na zewnątrz.

| Usługa | Adres |
|--------|--------|
| Skrzynka WWW (podgląd maili) | http://localhost:8025 |
| Serwer SMTP | `meow_mailpit:1025` (w Dockerze) / `localhost:1025` (lokalnie) |

Zmienne są ustawione w `docker-compose.yml` — po checkout w sklepie mail pojawi się w Mailpit.

## Produkcja (Railway) — Brevo API (zalecane)

**Railway blokuje port SMTP 587** — na hostingu używaj **API Brevo** (HTTPS, port 443).

1. Brevo → **SMTP & API** → zakładka **API Keys** → **Generate a new API key**
2. Skopiuj klucz (`xkeysib-...`)
3. Railway → serwis `meow-app` → **Variables**:

```
Smtp__ApiKey=xkeysib-...
Smtp__From=filip@studenci.collegiumwitelona.pl
Smtp__FromName=meow E-Księgarnia
```

4. W Brevo → **Senders** — ten sam adres co `Smtp__From` musi być **Verified** ✅

Zmienne `Smtp__User` / `Smtp__Password` (SMTP) zostaw dla lokalnego Dockera — na Railway **wystarczy `Smtp__ApiKey`**.

## Produkcja — Brevo SMTP (tylko Docker / VPS)

Port 587 działa lokalnie, nie na Railway:

```
Smtp__Provider=Brevo
Smtp__User=ae8e09001@smtp-brevo.com
Smtp__Password=xsmtpsib-...
Smtp__From=filip@studenci.collegiumwitelona.pl
Smtp__FromName=meow E-Księgarnia
```

## Gmail (hasło aplikacji)

1. Włącz 2FA w Google Account
2. Utwórz **Hasło aplikacji** dla „Poczta”
3. Variables:

```
Smtp__Provider=Gmail
Smtp__User=twoj@gmail.com
Smtp__Password=haslo-aplikacji-16-znakow
Smtp__From=twoj@gmail.com
```

## Lokalnie bez Docker (opcjonalnie)

Skopiuj szablon i uzupełnij dane (plik jest w `.gitignore`):

```powershell
copy appsettings.Smtp.example.json appsettings.Smtp.local.json
```

`Program.cs` ładuje `appsettings.Smtp.local.json` automatycznie.

## Mail nie dochodzi mimo 201 OK?

API Brevo zwraca sukces, ale skrzynka pusta — **99% to konto/nadawca w Brevo**, nie aplikacja.

### Checklist Brevo

1. **Senders** → `filip@studenci.collegiumwitelona.pl` → status **Verified** (kliknij link w mailu weryfikacyjnym od Brevo)
2. **Transactional** → **Logs** → wyszukaj `messageId` z logów Railway (np. `202606122311...`)
   - **Delivered** = poszło, szukaj w spamie
   - **Blocked / Invalid sender** = nadawca niezweryfikowany
   - **Brak wpisu** = konto Brevo w weryfikacji (`account under validation`)
3. **Ustawienia konta** — dokończ onboarding (telefon, firma), Brevo czasem blokuje wysyłkę na nowych kontach
4. **Test na Gmail** — dodaj Gmail w Senders, ustaw `Smtp__From` na Gmail, w checkout podaj Gmail jako odbiorcę

### Interia / skrzynki uczelniane

Interia i serwery uczelniane często **odrzucają** maile z Brevo bez wpisu do skrzynki. Na demo/obronę wystarczy:
- log Railway: `Wysłano e-mail (Brevo API)... messageId`
- screenshot logów **Transactional** w Brevo

## Weryfikacja

- Brak `Smtp:Host` i `Smtp:Provider` → maile trafiają do logów jako `[MAIL-DEV]`
- Po konfiguracji → złóż testowe zamówienie w sklepie z prawidłowym adresem e-mail klienta
- Logi Railway: `.\.tools\railway.exe logs --service meow-app`
