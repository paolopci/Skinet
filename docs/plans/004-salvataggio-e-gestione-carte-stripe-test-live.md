# Salvataggio e Gestione Carte Stripe (Test/Live)

## Data (ISO)
2026-02-08

## Scope
Implementazione end-to-end del salvataggio carte Stripe per utenti autenticati, gestione lista/default/eliminazione lato API e integrazione client Angular checkout/account con isolamento per utente.

## Checklist step
- [x] Definire comportamento funzionale checkout e gestione carte.
- [x] Disegnare mapping utente-Stripe Customer.
- [x] Estendere API per lista/default/delete metodi salvati.
- [x] Aggiornare create/update PaymentIntent con savePaymentMethod e paymentMethodId.
- [x] Aggiornare Angular checkout e nuova pagina account carte salvate.
- [x] Estendere test Postman/Newman sui nuovi endpoint.
- [x] Validare sicurezza ownership e regressione build/test.

## Stato finale per step
- Step 1: completed
- Step 2: completed
- Step 3: completed
- Step 4: completed
- Step 5: completed
- Step 6: completed
- Step 7: completed

## Verifiche eseguite (build/test)
- `dotnet build Skinet.sln` ✅
- `dotnet test Skinet.sln --no-build` ✅
- `npm run build --prefix client` ✅ (warning budget Angular preesistente)

## File toccati
- `Core/Entities/AppUser.cs`
- `Core/Interfaces/IPaymentService.cs`
- `Core/Payments/PaymentIntentOperationResult.cs`
- `Core/Payments/SavedPaymentMethodResult.cs`
- `API/DTOs/CreateOrUpdatePaymentIntentRequest.cs`
- `API/DTOs/SavedPaymentMethodResponse.cs`
- `API/Controllers/PaymentsController.cs`
- `Infrastructure/Data/StoreContext.cs`
- `Infrastructure/Services/PaymentService.cs`
- `Infrastructure/Migrations/20260208104019_AddStripeCustomerIdToAppUser.cs`
- `Infrastructure/Migrations/20260208104019_AddStripeCustomerIdToAppUser.Designer.cs`
- `Infrastructure/Migrations/StoreContextModelSnapshot.cs`
- `client/src/app/core/services/stripe.service.ts`
- `client/src/app/core/services/payment-methods.service.ts`
- `client/src/app/shared/models/saved-payment-method.ts`
- `client/src/app/features/checkout/checkout.component.ts`
- `client/src/app/features/checkout/checkout.component.html`
- `client/src/app/features/account/payment-methods/payment-methods.component.ts`
- `client/src/app/features/account/payment-methods/payment-methods.component.html`
- `client/src/app/features/account/payment-methods/payment-methods.component.scss`
- `client/src/app/app.routes.ts`
- `client/src/app/layout/header/header.component.html`
- `Scripts/Payments/Skinet-Payments.postman_collection.json`
- `Scripts/Payments/Skinet-Payments.postman_environment.json`
