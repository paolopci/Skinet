# Stato Client / UI

Data aggiornamento: 2026-02-08

## Stato attuale
- Loader globale con `LoadingService` e `loadingInterceptor` (include `delay(500)` temporaneo per test visivi).
- Progress bar Material sotto l'header, visibile solo su route `/shop` quando `loading$` è attivo.
- Overlay full-screen mostrato solo fuori da `/shop` quando `loading$` è attivo.
- Header sticky con effetto glass (`bg-white/95`, `backdrop-blur`, ombra responsive).
- Pagina NotFound completata con icona, testo 404 e CTA `Back to shop`.

## File principali
- `client/src/app/app.component.ts`
- `client/src/app/app.component.html`
- `client/src/app/app.component.scss`
- `client/src/app/core/services/loading.service.ts`
- `client/src/app/core/interceptors/loading-interceptor.ts`
- `client/src/app/layout/header/header.component.html`
- `client/src/app/shared/components/not-found/not-found.component.ts`
- `client/src/app/shared/components/not-found/not-found.component.html`
- `client/src/app/shared/components/not-found/not-found.component.scss`

## Verifica rapida manuale
- `npm run start --prefix client`
- Route `/shop`: progress bar visibile durante il caricamento delle card.
- Route `/test-error`: overlay full-screen visibile fuori da `/shop`.
