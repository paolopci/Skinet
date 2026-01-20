# 🛒 Skinet – Full‑Stack Demo (.NET 9 + Angular 21)

Progetto full‑stack basato su **ASP.NET Core 9 Web API** e **Angular 21 (standalone)**, organizzato secondo i principi di **Clean Architecture**.
Il progetto è utilizzato come base didattica (stile corso *Skinet*) e come template professionale per applicazioni enterprise.

---

## 🧱 Architettura della Solution

```
Skinet/
 ├─ API/              → ASP.NET Core 9 Web API (host)
 ├─ Core/             → Dominio (Entities, Interfaces)
 ├─ Infrastructure/   → Implementazioni (EF, servizi esterni)
 ├─ tests/            → Progetti di test (xUnit – da estendere)
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
cd client
npm start
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

Convenzioni:

* Progetti test: `*.Tests`
* Framework: xUnit
* Naming: `Metodo_Risultato_Condizione`

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

* [ ] Autenticazione JWT
* [ ] Catalogo prodotti
* [ ] Basket e Checkout
* [ ] SignalR Notifications
* [ ] Docker Compose
* [ ] CI/CD Pipeline

---

## 👤 Autore

Progetto sviluppato da **Paolo Paci** come esercizio avanzato su stack **.NET + Angular** con approccio enterprise.

---

> Per regole operative dell’AI, vedere: **AGENTS.md**
