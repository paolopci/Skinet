# Gestione Ordini Lato Client (Menu + Lista Paginata + Filtri)

## Data (ISO)

2026-02-08

## Scope

- Abilitare la voce `My orders` nel menu utente solo se l'utente ha ordini.
- Aggiungere pagina ordini autenticata con ordinamento data, filtri trimestre/anno, ricerca per data o `orderId`.
- Normalizzare sempre il risultato in formato `{ orders, pagination }` lato client.

## Checklist step

- [x] Definizione modelli ordini client (`OrderListItem`, `OrdersPagination`, `OrdersResponse`, `OrdersQueryParams`)
- [x] Implementazione `OrdersService` con adapter robusto e paginazione/filtering client-side
- [x] Implementazione feature `account/orders` (component TS/HTML/SCSS)
- [x] Aggiornamento routing con guard autenticazione
- [x] Integrazione header desktop/mobile con stato dinamico `hasOrders`
- [x] Invalidazione cache ordini su checkout confermato
- [x] Verifiche finali build/test

## Stato finale per step

- Definizione modelli ordini client: `completed`
- Implementazione `OrdersService`: `completed`
- Implementazione feature `account/orders`: `completed`
- Aggiornamento routing: `completed`
- Integrazione header: `completed`
- Invalidazione cache ordini: `completed`
- Verifiche finali build/test: `completed`

## Verifiche eseguite (build/test)

- `npm run build --prefix client` (OK).
- Nota: warning budget Angular bundle iniziale (`1.07 MB` > `900 kB`), nessun errore bloccante.

## File toccati

- `client/src/app/shared/models/order-list-item.ts`
- `client/src/app/shared/models/orders-pagination.ts`
- `client/src/app/shared/models/orders-response.ts`
- `client/src/app/shared/models/orders-query-params.ts`
- `client/src/app/core/services/orders.service.ts`
- `client/src/app/features/account/orders/orders.component.ts`
- `client/src/app/features/account/orders/orders.component.html`
- `client/src/app/features/account/orders/orders.component.scss`
- `client/src/app/app.routes.ts`
- `client/src/app/layout/header/header.component.ts`
- `client/src/app/layout/header/header.component.html`
- `client/src/app/features/checkout/checkout.component.ts`
