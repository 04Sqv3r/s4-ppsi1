# Dokumentacja projektowa — meow

## Sprawozdanie (LaTeX → PDF)

Plik: [`sprawozdanie.tex`](sprawozdanie.tex)

### Kompilacja lokalnie

**MiKTeX / TeX Live:**

```bash
cd docs
pdflatex sprawozdanie.tex
pdflatex sprawozdanie.tex
```

Wynik: `sprawozdanie.pdf`

### Online (bez instalacji TeX)

1. Wejdź na [Overleaf](https://www.overleaf.com)
2. New Project → Upload Project → wrzuć `sprawozdanie.tex`
3. Menu: Compiler → **pdfLaTeX** → Recompile

### Zawartość sprawozdania (aktualna)

- Opis przedmiotu zlecenia
- Opis technologiczny rozwiązania
- Wykaz zadań z szacowanym czasem
- **Podział zadań w zespole**
- Instrukcja uruchomienia lokalnego i zdalnego (Railway)
- Dokumentacja OpenAPI (Swagger) — URL lokalny i produkcyjny

### Demo produkcyjne

- WWW: https://meow-app-production.up.railway.app
- Swagger: https://meow-app-production.up.railway.app/swagger

### Powiązane pliki

- [SMTP.md](SMTP.md) — mailing (Brevo API, Mailpit)
- [../DEPLOY.md](../DEPLOY.md) — wdrożenie
- [../README.md](../README.md) — README repozytorium
