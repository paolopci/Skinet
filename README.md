# 🛒 Skinet – Full‑Stack Demo (.NET 9 + Angular 21)

![.NET 9](https://img.shields.io/badge/.NET-9-512BD4?style=flat&logo=dotnet&logoColor=white)
![Angular 21](https://img.shields.io/badge/Angular-21-DD0031?style=flat&logo=angular&logoColor=white)

Progetto full‑stack basato su **ASP.NET Core 9 Web API** e **Angular 21 (standalone)**, organizzato secondo i principi di **Clean Architecture**.
Il progetto è utilizzato come base didattica (stile corso *Skinet*) e come template professionale per applicazioni enterprise.

---

## 🧱 Architettura della Solution

```
Skinet/
 ├─ API/              → ASP.NET Core 9 Web API (host)
 ├─ Core/             → Dominio (Entities, Interfaces)
 ├─ Infrastructure/   → Implementazioni (EF, servizi esterni)
 └─ client/           → Angular 21 standalone (SPA)
```

### Principi adottati

* Clean Architecture
* Dependency Injection
* RESTful API
* JWT Authentication (in roadmap)
* Repository Pattern (valutato per necessità)

---

## 🔧 Requisiti

### Backend

* .NET SDK 9
* Visual Studio 2026 (consigliato)

### Frontend

* Node.js LTS
* npm
* Angular CLI (usata via `npx`)
* Visual Studio Code (consigliato)

---

## 🚀 Avvio rapido

Avvio completo (API + client) dalla root del repository:

```bash
dotnet run --project API/API.csproj
npm install --prefix client
npm run start --prefix client
```

* API: porte in `API/Properties/launchSettings.json`
* Client: `http://localhost:4200`

---

### ▶ Avvio API (.NET)

Dalla root del repository:

```bash
dotnet run --project API/API.csproj
```

Oppure in Visual Studio:

* Imposta **API** come Startup Project
* Premi **F5** (Debug)

L’API sarà disponibile sulle porte definite in:

```
API/Properties/launchSettings.json
```

---

### ▶ Avvio Client Angular

```bash
npm install --prefix client
npm run start --prefix client
```

L’app Angular sarà disponibile su:

```
http://localhost:4200
```

---

## 🔁 Comunicazione Angular ↔ API

Durante lo sviluppo, Angular usa un **proxy** per inoltrare le richieste verso la Web API evitando problemi CORS.

### File: `client/proxy.conf.json`

```json
{
  "/api": {
    "target": "https://localhost:5001",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

### Avvio con proxy

Nel `package.json`:

```json
"start": "ng serve --proxy-config proxy.conf.json"
```

### Base URL per ambienti

Attualmente il client usa un base URL fisso in `client/src/app/core/services/shop.service.ts`.
Esempi consigliati:

```text
Dev (con proxy): /api
Dev (diretto): https://localhost:5001/api/
Prod: https://<tuo-dominio>/api/
```

Se vuoi renderlo configurabile, sposta il base URL negli environment Angular.

### Chiamate HTTP lato Angular

```ts
this.http.get<Product[]>('/api/products');
```

---

## 🧪 Test

Per eseguire i test:

```bash
dotnet test Skinet.sln
```

Nota operativa:

* Se la build fallisce con “Accesso negato”, rilancia la build con permessi elevati.
* Se un processo `API` blocca `API.exe`, termina il processo e rilancia la build con permessi elevati.

Convenzioni:

* Progetti test: `*.Tests`
* Framework: xUnit
* Naming: `Metodo_Risultato_Condizione`

---

## 🧩 Struttura API

Endpoint principali:

```text
GET    /api/products?brands=brand1,brand2&types=type1,type2&sort=priceAsc|priceDesc&pageIndex=1&pageSize=10
GET    /api/products/{id}
GET    /api/products/brands
GET    /api/products/types
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

Endpoint di test errori (diagnostica):

```text
GET  /api/buggy/unauthorized
GET  /api/buggy/forbidden
GET  /api/buggy/not-found
GET  /api/buggy/status-550
GET  /api/buggy/server-error
GET  /api/buggy/redirect-products
GET  /api/buggy/service-unavailable
POST /api/buggy/validationerror
```

Endpoint demo:

```text
GET /WeatherForecast
```

---

## 🧯 Troubleshooting

* Build “Accesso negato”: rilancia con permessi elevati.
* `API.exe` bloccato: termina il processo `API` e rilancia la build con permessi elevati.
* HTTPS dev non valido: rigenera i certificati di sviluppo.
* CORS/proxy: verifica `client/proxy.conf.json`.

---

## 🧠 Uso con Codex / AI Agents

Il progetto include un file **AGENTS.md** con:

* ruoli richiesti all’AI
* regole architetturali
* flusso operativo (checklist + conferme)
* linee guida di build e sicurezza

👉 Codex CLI utilizza **AGENTS.md** come fonte primaria di istruzioni.
👉 `README.md` è invece pensato per sviluppatori umani e documentazione del progetto.

---

## 📌 Roadmap (indicativa)

* [x] Catalogo prodotti
* [ ] Autenticazione JWT
* [ ] Basket e Checkout
* [ ] SignalR Notifications
* [ ] Docker Compose
* [ ] CI/CD Pipeline

---

## 👤 Autore

Progetto sviluppato da **Paolo Paci** come esercizio avanzato su stack **.NET + Angular** con approccio enterprise.

---

> Per regole operative dell’AI, vedere: **AGENTS.md**


