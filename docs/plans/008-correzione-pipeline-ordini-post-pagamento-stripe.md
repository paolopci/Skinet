# Correzione Pipeline Ordini Post-Pagamento Stripe

- Data: 2026-02-08
- Scope: allineare pipeline Stripe->ordine dominio creando record in `Ordini` e `DettaglioOrdine`, con idempotenza tra finalize e webhook.

## Checklist Step
- [x] Step 1 - Analisi flusso checkout/pagamento/menu ordini.
- [x] Step 2 - Verifica DB (`PaymentOrders` popolata, `Ordini`/`DettaglioOrdine` vuote).
- [x] Step 3 - Refactor `PaymentService` per doppia persistenza.
- [x] Step 4 - Idempotenza tra finalize e webhook su `PaymentIntentId`.
- [x] Step 5 - Aggiornamento schema (`PaymentOrders.OrderId` + FK a `Ordini`).
- [x] Step 6 - Test backend su creazione/link ordine dominio.
- [x] Step 7 - Build/test e verifica SQL finale.

## Stato Finale Step
- Step 1: completed
- Step 2: completed
- Step 3: completed
- Step 4: completed
- Step 5: completed
- Step 6: completed
- Step 7: completed

## Verifiche Eseguite
- `dotnet build Skinet.sln`
- `dotnet test Skinet.sln`
- Query SQL su `PaymentOrders`, `Ordini`, `DettaglioOrdine`

## File Toccati
- `Core/Entities/PaymentOrder.cs`
- `Infrastructure/Config/PaymentOrderConfiguration.cs`
- `Infrastructure/Services/PaymentService.cs`
- `Infrastructure/Migrations/20260208193804_AddOrderLinkToPaymentOrders.cs`
- `Infrastructure/Migrations/20260208193804_AddOrderLinkToPaymentOrders.Designer.cs`
- `Infrastructure/Migrations/StoreContextModelSnapshot.cs`
- `tests/Core.Tests/Core.Tests.csproj`
- `tests/Core.Tests/PaymentServiceOrderPipelineTests.cs`
- `docs/plans/008-correzione-pipeline-ordini-post-pagamento-stripe.md`

## Assunzioni
- Strategia storici: solo futuri (nessun backfill retroattivo).
- Nessuna modifica frontend necessaria oltre cache invalidation già presente.
