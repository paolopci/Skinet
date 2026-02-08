# Allineamento Carrello Post-Pagamento (Client/Server)

- Data (ISO): 2026-02-08
- Scope: allineare stato carrello client/server dopo pagamento confermato, con reset immediato e robustezza su errori.

## Checklist Step
- [x] Verifica stato attuale del flusso `finalize` e reset carrello lato client.
- [x] Aggiornamento `CartService.clearClientCartState()` con reset coerente storage scoped.
- [x] Svuotamento client immediato dopo `finalize` riuscito nel checkout.
- [x] Reset stato checkout (shipping + storage shipping) post pagamento confermato.
- [x] Robustezza su errori/retry: niente svuotamento se `finalize` fallisce.
- [x] Validazione tecnica completa (`dotnet test/build`, `npm test/build`).
- [x] Persistenza del piano implementato in `docs/plans/` con naming slug sicuro.

## Stato Finale Step
- Step 1: completed
- Step 2: completed
- Step 3: completed
- Step 4: completed
- Step 5: completed
- Step 6: completed
- Step 7: completed

## Verifiche Eseguite
- `dotnet test Skinet.sln` -> OK (5 test passati).
- `dotnet build Skinet.sln` -> OK (0 warning, 0 errori).
- `npm run test --prefix client -- --watch=false` -> OK (2 test passati).
- `npm run build --prefix client` -> OK (warning budget Angular non bloccante).

## File Toccati
- `client/src/app/core/services/cart.service.ts`
- `client/src/app/core/services/checkout.service.ts`
- `client/src/app/features/checkout/checkout.component.ts`
- `docs/plans/allineamento-carrello-post-pagamento-client-server.md`
