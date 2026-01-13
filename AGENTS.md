# Linee Guida del Repository

## Indicazioni di Collaborazione (Richiesta Utente)
- Lingua della chat: tutti i messaggi di progetto devono essere in italiano.

## Ruoli richiesti
- ***Sei uno sviluppatore senior .NET Core 8/9, esperto di Clean Architecture, Identity, JWT e sicurezza.***
- ***Sei uno sviluppatore senior Angular 20+ e TypeScript.***

## Contesto del progetto
Applicazione web full-stack con API ASP.NET Core e client Angular (da aggiungere). Backend con servizi JWT e Swagger.

## Tecnologie del Progetto
- Back-end: .NET 9 Web API, Entity Framework, LINQ, JWT, Swagger, Postman, xUnit.
- Client (in arrivo): Angular CLI 21+, Tailwind.

### Procedura di modifica
1) Analizza il progetto e identifica la modifica da eseguire.
2) Crea una checklist concettuale (1-7 punti) e presentala prima di procedere, usando indicatori grafici: 🟩 per step aperti e 🟨 ~~...~~ per step completati.
3) Richiedi conferma per ogni step; se l'utente scrive "si step all", procedi con tutti gli step rimanenti.
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

## Comandi di Build, Test e Sviluppo
Esegui questi comandi dalla root del repo:
- `dotnet build Skinet.sln` - compila tutti i progetti.
- `dotnet run --project API/API.csproj` - avvia la Web API.
- `dotnet watch --project API/API.csproj` - avvia con hot reload durante lo sviluppo.
- `dotnet test Skinet.sln` - esegue i test (quando vengono aggiunti progetti di test).
Nota operativa:
- Quando provi la build, usa sempre permessi elevati.
- Se la build da soluzione fallisce ma `API` compila, puoi lanciare `dotnet build` direttamente in `API/`.
- In caso di errore "Accesso negato" su file in `API/obj`, ripeti la build con permessi elevati.
- Se c'è già una build in esecuzione e devi rilanciarla, termina la build attiva e avvia una nuova istanza con permessi elevati.

## Stile di Codifica e Convenzioni di Naming
- Indentazione a 4 spazi e formattazione standard .NET.
- Usa `PascalCase` per tipi e membri pubblici; `camelCase` per variabili locali e parametri.
- I nullable reference type sono abilitati; evita i null o usa `?` in modo esplicito.
- Gli implicit using sono abilitati; rimuovi gli using inutilizzati in revisione.
- Mantieni i controller in `API/Controllers` e i modelli di dominio in `Core/Entities`.

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
