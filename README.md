## fitBarbs Web

Aplikacja ASP.NET Core (NET 8) dla platformy treningowej z frontem opartym o Tailwind CSS i lekkie komponenty własne.

### Szybki start (dev)
- Wymagania: .NET 8 SDK, Node 18+ (Tailwind)
- Instalacja deps (frontend):
  - `cd FitBarbs.Web && npm ci`
- Uruchomienie aplikacji:
  - `dotnet run --project FitBarbs.Web`
  - Domyślnie: `http://localhost:5174`

Tailwind buduje się automatycznie podczas kompilacji .NET (skrypt `npm run build` odpalany w trakcie builda). Do szybkiej iteracji możesz odpalić watch:

```bash
cd FitBarbs.Web
npx tailwindcss -c tailwind.config.js -i ./Styles/app.css -o ./wwwroot/css/app.css --watch
```

### Stylowanie: Tailwind only + komponenty własne
- Nie używamy już Bootstrap CSS. Zostaje tylko JS tam, gdzie niezbędny (obecnie nieużywany).
- Główny plik stylów: `FitBarbs.Web/Styles/app.css`
  - `@tailwind base; @tailwind components; @tailwind utilities;`
  - Warstwa `@layer components` zawiera gotowe komponenty (np. `.btn`, `.btn-primary`, `.card`, `.badge`).
- Konfiguracja: `FitBarbs.Web/tailwind.config.js` (źródła skanowane w `Views/**/*.cshtml`, `wwwroot/js/**/*.js`).

#### Konwencje
- Preferuj klasy narzędziowe Tailwind w widokach (`flex`, `grid`, spacing, kolory z configa).
- Komponenty powtarzalne dodawaj w `@layer components` w `Styles/app.css` i używaj krótkich nazw (`.btn-primary`, `.btn-secondary`, `.card`).
- Kolory/tonu używaj przez tokeny (CSS variables / theme): `var(--fb-*)` lub `theme.colors.brand` z configa.
- Formularze: proste inputy/selecty stylizujemy klasami Tailwind (zaokrąglenia, border, padding).

#### Przykłady
- Link w stopce: `text-neutral-900 hover:underline underline-offset-4 decoration-neutral-500/50`
- Przycisk główny: użyj `.btn-primary`
- Karty: użyj `.card` + utility (np. `p-4`, `h-full`)

### Struktura
- `FitBarbs.Web/Views/...` – Razor views (Tailwind klasy bezpośrednio w znacznikach)
- `FitBarbs.Web/Styles/app.css` – komponenty i warstwy Tailwind
- `FitBarbs.Web/wwwroot/css/app.css` – wynik kompilacji Tailwind (generowany, nie edytuj)

### Zasoby i pliki duże
- Katalogi publikacji i uploadów nie są wersjonowane:
  - ignorowane w `.gitignore`: `FitBarbs.Web/publish/`, `FitBarbs.Web/wwwroot/uploads/`
  - Nie commitujemy plików >100 MB. Dla dużych assetów użyj hostingu zewnętrznego lub Git LFS (jeśli kiedyś wdrożymy).

### Wkład (contributing)
- Trzymaj się Tailwind only. Jeśli widzisz klasy Bootstrap w widoku – refaktor do Tailwind.
- Nazewnictwo: krótkie, opisowe komponenty; reszta przez utility classes.
- Po zmianach odpal build: `dotnet build` (zbuduje też CSS). 


