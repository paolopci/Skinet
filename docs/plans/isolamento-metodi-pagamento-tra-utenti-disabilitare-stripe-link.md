# Isolamento Metodi Pagamento tra Utenti (Disabilitare Stripe Link)

- Data (ISO): 2026-02-08
- Scope: evitare che nello step Payment compaiano metodi Link salvati nel browser quando cambia utente applicativo.

## Checklist Step
- [x] Configurare `StripeService` per escludere Stripe Link nel Payment Element.
- [x] Implementare fallback robusto se la configurazione anti-Link non fosse supportata.
- [x] Verificare UX dello step Payment mantenendo errori/retry.
- [x] Confermare reset sessione Stripe al cambio utente con remount dell'elemento.
- [x] Eseguire validazione tecnica (`npm build` + `npm test`).
- [x] Creare file piano implementato in `docs/plans/`.
- [x] Riepilogo finale con esiti e scenari di smoke manuale.

## Stato Finale Step
- Step 1: completed
- Step 2: completed
- Step 3: completed
- Step 4: completed
- Step 5: completed
- Step 6: completed
- Step 7: completed

## Verifiche Eseguite
- `npm run build --prefix client` -> OK (warning budget non bloccante).
- `npm run test --prefix client -- --watch=false` -> OK (2 test passati).

## File Toccati
- `client/src/app/core/services/stripe.service.ts`
- `docs/plans/isolamento-metodi-pagamento-tra-utenti-disabilitare-stripe-link.md`

## Smoke Manuale (da eseguire)
1. Login utente A, checkout fino a step 3 (Payment).
2. Logout e login utente B nello stesso browser.
3. Checkout step 3 deve mostrare inserimento carta senza proposta Link salvata.
4. Verificare che payment retry continui a funzionare in caso di errore.
