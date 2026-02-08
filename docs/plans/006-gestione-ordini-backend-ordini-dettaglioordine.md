# Piano Implementazione Backend: Gestione Ordini (`Ordini` + `DettaglioOrdine`)

## Data (ISO)
2026-02-08

## Scope
- Introdurre un dominio ordini separato da `PaymentOrders`.
- Aggiungere CRUD completo via `OrdersController`.
- Applicare regole autorizzative admin/user con ownership.
- Supportare sorting/filtering richiesti su `GET /api/orders`.
- Restituire payload errore nel formato `{ "error": "..." }` per i casi richiesti.

## Checklist Step
- [x] STEP 1: analisi stato repository e schema DB reale
- [x] STEP 2: scelta strategia schema ordini (nuove tabelle dedicate)
- [x] STEP 3: scelta tipo FK utente (`UserId` string allineato Identity)
- [x] STEP 4: scelta policy permessi CRUD (admin tutto, user solo proprie risorse)
- [x] STEP 5: definizione contratti API + modelli request/response
- [x] STEP 6: definizione persistenza EF + migration + query/sorting/filter
- [x] STEP 7: definizione test/verifiche e criteri di accettazione

## Stato Finale Step
- STEP 1: completed
- STEP 2: completed
- STEP 3: completed
- STEP 4: completed
- STEP 5: completed
- STEP 6: completed
- STEP 7: completed

## Verifiche Eseguite (build/test)
- `dotnet build Skinet.sln` -> OK
- `dotnet test Skinet.sln` -> OK (5 test superati, 0 falliti)

## File Toccati
- `Core/Interfaces/ISpecification.cs`
- `Core/Specification/BaseSpecification.cs`
- `Infrastructure/Data/SpecificationEvaluator.cs`
- `Core/Entities/Order.cs`
- `Core/Entities/OrderDetail.cs`
- `Infrastructure/Config/OrderConfiguration.cs`
- `Infrastructure/Config/OrderDetailConfiguration.cs`
- `Infrastructure/Data/StoreContext.cs`
- `Core/Specification/OrderSpecParams.cs`
- `Core/Specification/OrderSpecification.cs`
- `Core/Specification/OrderCountSpecification.cs`
- `API/DTOs/OrderDetailDto.cs`
- `API/DTOs/OrderListItemDto.cs`
- `API/DTOs/OrderDetailsResponseDto.cs`
- `API/DTOs/CreateOrderRequestDto.cs`
- `API/DTOs/UpdateOrderRequestDto.cs`
- `API/DTOs/OrderQueryParamsDto.cs`
- `API/Controllers/OrdersController.cs`
- `Infrastructure/Migrations/20260208180854_AddOrdersAndOrderDetails.cs`
- `Infrastructure/Migrations/20260208180854_AddOrdersAndOrderDetails.Designer.cs`
- `Infrastructure/Migrations/StoreContextModelSnapshot.cs`
