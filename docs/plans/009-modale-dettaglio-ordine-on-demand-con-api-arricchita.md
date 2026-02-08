# Modale Dettaglio Ordine On-Demand con API Arricchita

- Data: 2026-02-08
- Scope: introdurre apertura dettaglio ordine tramite modale in area account e arricchire endpoint `GET /orders/{orderId}` con dati prodotto (nome/immagine).

## Checklist Step
- [x] Aggiornare DTO backend dettaglio ordine con `NomeProdotto` e `ImmagineUrl`.
- [x] Estendere dominio/infrastruttura con navigation `OrderDetail -> Product` e FK esplicita.
- [x] Aggiornare specification per include annidato `Details.Product`.
- [x] Aggiornare mapping controller con fallback prodotto storico non trovato.
- [x] Aggiungere modelli client per dettaglio ordine.
- [x] Estendere `OrdersService` con `getOrderDetails(orderId)` e mapping robusto.
- [x] Introdurre modale standalone `OrderDetailsDialogComponent` con loading/errore/retry.
- [x] Aggiungere azione `Dettagli` nella tabella ordini.
- [x] Creare migrazione EF e applicarla al DB locale.
- [x] Eseguire build/test finali.

## Stato Finale per Step
- Step 1: completed
- Step 2: completed
- Step 3: completed
- Step 4: completed
- Step 5: completed
- Step 6: completed
- Step 7: completed
- Step 8: completed
- Step 9: completed
- Step 10: completed

## Verifiche Eseguite
- `dotnet build Skinet.sln`
- `dotnet test Skinet.sln`
- `npm run build --prefix client`
- `dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj`

## File Toccati
- `API/DTOs/OrderDetailDto.cs`
- `API/Controllers/OrdersController.cs`
- `Core/Entities/OrderDetail.cs`
- `Core/Interfaces/ISpecification.cs`
- `Core/Specification/BaseSpecification.cs`
- `Core/Specification/OrderSpecification.cs`
- `Infrastructure/Config/OrderDetailConfiguration.cs`
- `Infrastructure/Data/SpecificationEvaluator.cs`
- `Infrastructure/Migrations/20260208200259_AddOrderDetailProductRelationship.cs`
- `Infrastructure/Migrations/20260208200259_AddOrderDetailProductRelationship.Designer.cs`
- `Infrastructure/Migrations/StoreContextModelSnapshot.cs`
- `client/src/app/shared/models/order-detail-item.ts`
- `client/src/app/shared/models/order-details-response.ts`
- `client/src/app/core/services/orders.service.ts`
- `client/src/app/features/account/orders/orders.component.ts`
- `client/src/app/features/account/orders/orders.component.html`
- `client/src/app/features/account/orders/order-details-dialog/order-details-dialog.component.ts`
- `client/src/app/features/account/orders/order-details-dialog/order-details-dialog.component.html`
- `client/src/app/features/account/orders/order-details-dialog/order-details-dialog.component.scss`
- `docs/plans/009-modale-dettaglio-ordine-on-demand-con-api-arricchita.md`
