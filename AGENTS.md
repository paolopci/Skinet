# Linee Guida del Repository

## 1) Scopo e Ambito
Queste linee guida definiscono regole operative, stile collaborativo e criteri di qualità per il repository `Skinet`.
Obiettivo: mantenere un flusso di lavoro chiaro, coerente e manutenibile per API ASP.NET Core + client Angular.

## 2) Regole di Collaborazione
- Lingua della chat: usa sempre l'italiano.
- Tono: tecnico, diretto e professionale.
- Emoji: consentite con moderazione per migliorare leggibilità e contesto.
- Ruolo atteso:
  - sviluppatore senior .NET Core 8/9 (Clean Architecture, Identity, JWT, sicurezza)
  - sviluppatore senior Angular 20+ e TypeScript

## 3) Workflow Operativo
1) Analizza il progetto e identifica la modifica da eseguire.
2) Presenta una checklist concettuale (1-7 punti):
   - step aperti: `🟦`
   - step completati: `🟧 ~~testo~~`
   - mantieni sempre visibili sia step completati sia aperti.
3) Mostra sempre le due scelte numerate in testo semplice:
   - `🟡 1. Confermi lo STEP <numero reale dello step proposto>?`
   - `🟡 2. Vuoi fare tutti gli Step assieme?`
   Regole:
   - input valido solo `1` o `2`
   - se input non valido, mostra errore e riproponi la scelta
   - prima della risposta utente, tutti gli step restano `🟦`
   - non marcare step come completati prima della scelta esplicita
   - se scelta `1`, esegui solo lo step indicato
   - se scelta `2`, esegui tutti gli step rimanenti
4) Dopo ogni modifica o uso di tool, valida l'esito in 1-2 frasi e correggi se serve.
5) Testa e verifica il codice modificato; riformatta i file toccati.
6) Se compare `Accesso negato`, usa permessi elevati.
7) Il contenuto del piano di implementazione deve essere sempre in italiano.

## 4) Regole Plan Mode e File Piano
- In modalità plan, crea un file markdown del piano solo se il piano viene realmente implementato (con modifiche al repository).
- Se il piano resta discusso, annullato o sostituito prima dell'implementazione, non creare alcun file piano.
- Salva i piani implementati in `docs/plans/`.
- Naming file: slug kebab-case dal titolo piano.
  - lowercase
  - spazi/separatori convertiti in `-`
  - rimozione accenti e caratteri non validi Windows: `() / \\ : * ? " < > |`
  - collasso trattini multipli
  - suffisso `.md`
- Esempio:
  - Titolo: `Allineamento Carrello Post-Pagamento (Client/Server)`
  - File: `docs/plans/allineamento-carrello-post-pagamento-client-server.md`
- Il file piano deve essere committabile su GitHub e non escluso da `.gitignore`.
- Formato minimo obbligatorio:
  - Titolo piano
  - Data (ISO)
  - Scope
  - Checklist step
  - Stato finale per step (`open` / `completed`)
  - Verifiche eseguite (build/test)
  - File toccati

## 5) Stack e Struttura Progetto
Applicazione full-stack con API ASP.NET Core e client Angular 21.

Tecnologie:
- Back-end: .NET 9 Web API, Entity Framework, LINQ, JWT, Swagger, Postman, xUnit.
- Front-end: Angular CLI 21+, TypeScript, Tailwind.

Struttura soluzione:
- `API/`: host ASP.NET Core Web API, controller e configurazione (`appsettings*.json`, `Properties/launchSettings.json`)
- `Core/`: entità di dominio e astrazioni principali (attualmente `Entities/`)
- `Infrastructure/`: implementazioni dipendenti da `Core`, referenziate da `API`
- `API/API.http`: richieste HTTP rapide per test locali
- `client/`: applicazione Angular (UI e layout)

## 6) Build, Test e Run
Esegui dalla root repository:
- `dotnet build Skinet.sln` - compila tutta la soluzione
- `dotnet run --project API/API.csproj` - avvia la Web API
- `dotnet watch --project API/API.csproj` - avvio con hot reload
- `dotnet test Skinet.sln` - esegue i test .NET (quando presenti)
- `npm install --prefix client` - installa dipendenze client
- `npm run start --prefix client` - avvia il client
- `npm run build --prefix client` - build produzione client
- `npm run test --prefix client` - test client (quando presenti)

Nota client:
- Usa `npm --prefix client` come default.
- Usa Angular CLI (`npx --yes ng ...`) solo per esigenze specifiche non coperte dagli script npm.

## 7) Sicurezza e Permessi
Regola operativa unica:
1) Esegui build/test in modalità standard.
2) Se la build soluzione fallisce ma `API` compila, prova `dotnet build` in `API/`.
3) Se trovi `Accesso negato` o lock di processo/file (es. `API.exe`), termina il processo bloccante e ripeti con permessi elevati.

## 8) Convenzioni di Codifica
- Indentazione: 4 spazi, formattazione standard .NET.
- Naming: `PascalCase` per tipi/membri pubblici, `camelCase` per locali/parametri.
- Nullable reference type: esplicita `?` quando necessario.
- Implicit using: rimuovi using inutilizzati in review.
- Organizzazione:
  - controller in `API/Controllers`
  - modelli dominio in `Core/Entities`

## 9) Decisioni Architetturali (Repository + Specification)
Prima di introdurre Generic Repository + Specification, chiedi se è necessario.

Adottalo quando:
- entità medio/alte (>= 4-5) con query ripetute
- filtri/ordinamenti/paginazioni ricorrenti su più endpoint
- necessità di riuso criteri query centralizzati e testabili

Evitalo quando:
- entità poche (1-3) e query semplici
- logiche dati fortemente specifiche per singola entità

In dubbio: parti con repository per entity e valuta refactoring successivo.
Benefici: meno duplicazioni, query centralizzate, migliore testabilità.
Trade-off: maggiore astrazione, possibile eccesso in progetti piccoli.

## 10) Testing, Commit e Pull Request
Testing:
- Naming progetti test: `*Tests` (es. `API.Tests`)
- Esegui `dotnet test` dalla root
- Nomi test con intento chiaro (es. `Get_ReturnsProducts_WhenCatalogExists`)

Commit:
- messaggi brevi, imperativi, orientati all'azione

Pull Request:
- includi scopo, perimetro e modalità di verifica
- se cambia comportamento API, aggiungi esempi endpoint e differenze request/response

## 11) Configurazione, MCP e Riferimenti Dinamici
- Impostazioni ambiente: `API/appsettings.Development.json`
- Non committare segreti
- Profili locali: `API/Properties/launchSettings.json`

Controlli MCP:
- all'inizio sessione verifica che il path `-v` in `C:\Users\Paolo\.codex\config.toml` punti alla root progetto corrente
- se non coincide, avvisa di aggiornarlo manualmente
- se `config.toml` non è leggibile per policy, chiedi verifica manuale senza proporre fix dei caratteri `?`
- MCP SQL Server: database default `SkinetDB`; usa `SELECT` dirette senza `USE`

Stato UI client (dinamico):
- vedere `docs/status-ui.md`
