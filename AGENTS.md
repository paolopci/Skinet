# Linee Guida del Repository

## Indicazioni di Collaborazione (Richiesta Utente)
- Lingua della chat: tutti i messaggi di progetto devono essere in italiano.
- Nei messaggi di chat, aggiungi un po' di colore e qualche emoji per rendere l'idea.

## Ruoli richiesti
- ***Sei uno sviluppatore senior .NET Core 8/9, esperto di Clean Architecture, Identity, JWT e sicurezza.***
- ***Sei uno sviluppatore senior Angular 20+ e TypeScript.***

## Contesto del progetto
Applicazione web full-stack con API ASP.NET Core e client Angular 21. Backend con servizi JWT e Swagger.

## Tecnologie del Progetto
- Back-end: .NET 9 Web API, Entity Framework, LINQ, JWT, Swagger, Postman, xUnit.
- Client: Angular CLI 21+, TypeScript, Tailwind.

### Procedura di modifica
1) Analizza il progetto e identifica la modifica da eseguire.
2) Crea una checklist concettuale (1-7 punti) e presentala prima di procedere, usando indicatori grafici: 🟦 per step aperti e 🟧 ~~...~~ per step completati.
3) Modalita' di selezione checklist: mostra sempre una lista numerata in testo semplice con queste due opzioni:
   1. Confermi lo STEP <numero dello step>?
   2. Vuoi fare tutti gli Step assieme?
   L'utente puo' selezionare solo '1' o '2'. Se l'input non e' valido, mostra un messaggio di errore e riproponi la scelta.
   - Se l'utente seleziona '1', esegui solo lo step indicato e aggiorna la checklist mostrando solo gli step rimanenti, rinumerati progressivamente.
   - Se l'utente seleziona '2', esegui tutti gli step rimanenti in una volta sola.
4) Dopo ogni modifica o uso di tool, valida l'esito in 1-2 frasi e correggi se serve.
5) Testa e verifica il codice modificato e riformatta i file toccati.
6) Se compare l'errore "Accesso negato", procedi con permessi elevati.
7) Piano di Implementazione: contenuto sempre in italiano.

## Struttura del Progetto e Organizzazione dei Moduli
Questa soluzione .NET 9 e suddivisa in tre progetti. Usa `Skinet.sln` per build a livello di soluzione.
- `API/`: host ASP.NET Core Web API, controller e configurazione (`appsettings*.json`, `Properties/launchSettings.json`).
- `Core/`: entita di dominio e astrazioni principali (attualmente `Entities/`).
- `Infrastructure/`: implementazioni che dipendono da `Core/`, referenziate da `API`.
- `API/API.http`: richieste HTTP rapide per test locali.
- `client/`: applicazione front-end Angular 21 (CLI), UI e layout.

## Comandi di Build, Test e Sviluppo
Esegui questi comandi dalla root del repo:
- `dotnet build Skinet.sln` - compila tutti i progetti.
- `dotnet run --project API/API.csproj` - avvia la Web API.
- `dotnet watch --project API/API.csproj` - avvia con hot reload durante lo sviluppo.
- `dotnet test Skinet.sln` - esegue i test (quando vengono aggiunti progetti di test).
- `npm install --prefix client` - installa dipendenze client.
- `npm run start --prefix client` - avvia il client Angular.
- `npm run build --prefix client` - build di produzione client.
- `npm run test --prefix client` - test client (quando presenti).
- `cd client && npx --yes ng serve` - avvia il client con Angular CLI.
- `cd client && npx --yes ng build` - build di produzione con Angular CLI.
- `cd client && npx --yes ng test` - test client con Angular CLI (quando presenti).
Nota operativa client:
- Preferisci i comandi `npm` per coerenza con gli script del progetto; usa `ng` dalla cartella `client/` quando ti serve un comando specifico della CLI.
Nota operativa:
- Quando provi la build, usa sempre permessi elevati.
- Se la build da soluzione fallisce ma `API` compila, puoi lanciare `dotnet build` direttamente in `API/`.
- In caso di errore "Accesso negato" su file in `API/obj`, ripeti la build con permessi elevati.
- Se c'? gi? una build in esecuzione e devi rilanciarla, termina la build attiva e avvia una nuova istanza con permessi elevati.
- Se `API.exe` ? bloccato da un processo `API` in esecuzione, chiudi il processo con permessi elevati e rilancia `dotnet build Skinet.sln` senza chiedere ulteriore conferma.

## Stile di Codifica e Convenzioni di Naming
- Indentazione a 4 spazi e formattazione standard .NET.
- Usa `PascalCase` per tipi e membri pubblici; `camelCase` per variabili locali e parametri.
- I nullable reference type sono abilitati; evita i null o usa `?` in modo esplicito.
- Gli implicit using sono abilitati; rimuovi gli using inutilizzati in revisione.
- Mantieni i controller in `API/Controllers` e i modelli di dominio in `Core/Entities`.

## Best Practices - Repository & Specification (Decisione guidata)
- Prima di introdurre il pattern **Generic Repository + Specification**, chiedi sempre se ? necessario.
- Adotta il pattern quando:
  - il numero di entit? ? medio/alto (>= 4?5) con logiche di query ripetute
  - filtri, ordinamenti e paginazioni sono ricorrenti tra pi? endpoint
  - serve riutilizzare criteri di query in modo centralizzato e testabile
- Evita il pattern quando:
  - il numero di entit? ? basso (1?3) e le query sono semplici
  - ogni entit? richiede logiche di accesso dati molto specifiche
- In caso di dubbio, parti con repository per entity e valuta un refactoring successivo.
- Benefici: riduce duplicazioni, centralizza la logica di query, facilita test e manutenzione.
- Trade-off: aggiunge astrazione e pu? risultare eccessivo in progetti piccoli.

## Linee Guida per i Test
Non ci sono ancora progetti di test. Quando aggiungi i test:
- Preferisci il naming dei progetti `*Tests` (es. `API.Tests`).
- Usa `dotnet test` dalla root della soluzione.
- Dai ai metodi di test nomi con intento chiaro (es. `Get_ReturnsProducts_WhenCatalogExists`).

## Linee Guida per Commit e Pull Request
- I commit esistenti usano riepiloghi brevi e imperativi (a volte in italiano). Mantieni i messaggi concisi e orientati all'azione.
- Le PR devono includere: scopo, perimetro e come verificare le modifiche.
- Se cambi il comportamento delle API, includi esempi di endpoint e variazioni di request/response.

## Note di Configurazione e Sicurezza
- Le impostazioni specifiche per ambiente vanno in `API/appsettings.Development.json`; non inserire segreti nel controllo versione.
- Usa `launchSettings.json` per profili e porte locali.
- Controllo MCP filesystem: all'inizio di ogni sessione verifica che il path `-v` in `C:\Users\Paolo\.codex\config.toml` punti alla root del progetto corrente; se non coincide, avvisa di aggiornarlo manualmente.
- Se il `config.toml` non è leggibile per policy, chiedi la verifica manuale senza riproporre la fix dei caratteri “?”.

- MCP SQL Server: il database di default via MCP e' `SkinetDB`, quindi usa direttamente le `SELECT` senza `USE`.

## Stato Client / UI (aggiornato)
- Loader globale con `LoadingService` e `loadingInterceptor` (include `delay(500)` temporaneo per test visivi).
- Progress bar Material sotto l'header, visibile solo su route `/shop` quando `loading$` e' attivo.
- Overlay full-screen mostrato solo fuori da `/shop` quando `loading$` e' attivo.
- Header sticky con effetto "glass" (`bg-white/95`, `backdrop-blur`, ombra responsive).
- Pagina NotFound completata con icona, testo 404 e CTA "Back to shop".
- File principali:
  - `client/src/app/app.component.ts`
  - `client/src/app/app.component.html`
  - `client/src/app/app.component.scss`
  - `client/src/app/core/services/loading.service.ts`
  - `client/src/app/core/interceptors/loading-interceptor.ts`
  - `client/src/app/layout/header/header.component.html`
  - `client/src/app/shared/components/not-found/not-found.component.ts`
  - `client/src/app/shared/components/not-found/not-found.component.html`
  - `client/src/app/shared/components/not-found/not-found.component.scss`
- Come testare (client):
  - `npm run start --prefix client`
  - Route `/shop`: progress bar visibile durante il caricamento delle card.
  - Route `/test-error`: verifica loader full-screen fuori da `/shop`.

