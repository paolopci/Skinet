# Stripe Step 3 - Rollout e Runbook Operativo

## Checklist deploy
- Configurare `StripeSettings:SecretKey` in ambiente target (`sk_test_*` o `sk_live_*`).
- Configurare `StripeSettings:WebhookSecret` ottenuto dall'endpoint webhook Stripe (`whsec_*`).
- Verificare `StripeSettings:Currency` coerente con catalogo prezzi.
- Configurare `STRIPE_PUBLIC_KEY` e `STRIPE_MODE` lato client (`assets/env.js`).
- Verificare routing pubblico endpoint webhook: `POST /api/payments/webhook`.
- Applicare migrazioni DB includendo tabella `PaymentOrders`.
- Eseguire smoke test con carta Stripe test `4242 4242 4242 4242`.

## Osservabilita e logging
- Correlation fields presenti nei log payment API:
- `TraceId`
- `CartId`
- `PaymentIntentId`
- `UserId`
- Eventi principali da monitorare:
- `Richiesta create/update PaymentIntent ricevuta`
- `Richiesta finalize payment ricevuta`
- `Webhook Stripe ricevuto`
- `Finalize payment completata con successo`
- `Webhook processed: payment_intent.succeeded`

## Runbook incidenti

### 1. Webhook non ricevuti
- Verificare in Stripe Dashboard lo stato consegna eventi e HTTP status.
- Controllare DNS/SSL/reverse proxy dell'endpoint `/api/payments/webhook`.
- Validare `WebhookSecret` in configurazione API.
- Eseguire replay evento da Stripe Dashboard dopo fix.

### 2. Pagamento riuscito su Stripe ma ordine assente
- Cercare log con `PaymentIntentId` e `CartId`.
- Chiamare endpoint `POST /api/payments/{cartId}/finalize` con `paymentIntentId`.
- Verificare presenza record in tabella `PaymentOrders`.
- Se necessario, replay webhook `payment_intent.succeeded`.

### 3. Stato ordine incoerente (`Failed` dopo `Paid`)
- Verificare ordine eventi webhook Stripe e retry.
- Controllare log su `ResolveStatus` (status `Paid` non deve regredire).
- Se incoerenza persiste, aggiornare manualmente record con audit.

### 4. Errori firma webhook
- Controllare header `Stripe-Signature` ricevuto dall'app.
- Verificare secret attivo e ambiente corretto (test/live).
- Rigenerare endpoint secret e aggiornare configurazione.

## Verifica post-rilascio
- Creazione/aggiornamento `PaymentIntent` da checkout.
- Conferma pagamento da step Payment e transizione a Confirmation.
- Record `PaymentOrders` creato con stato `Paid` su pagamento riuscito.
- Cancellazione carrello dopo finalize/webhook succeeded.
