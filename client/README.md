# Client

Questo progetto e' stato generato con [Angular CLI](https://github.com/angular/angular-cli) versione 21.1.0.

## Server di sviluppo

Per avviare un server di sviluppo locale, usa:

```bash
npm run start --prefix client
```

Una volta avviato, apri il browser su `http://localhost:4200/`. L'applicazione si ricarica automaticamente quando modifichi i file sorgenti.

## Generazione codice

Angular CLI include tool di scaffolding. Per generare un componente:

```bash
cd client && npx --yes ng generate component nome-componente
```

Per la lista completa degli schematics:

```bash
cd client && npx --yes ng generate --help
```

## Build

Per compilare il progetto:

```bash
npm run build --prefix client
```

Il build produce gli artefatti nella cartella `dist/`.

## Test unitari

Per eseguire i test (quando presenti):

```bash
npm run test --prefix client
```

## Test end-to-end

Per test e2e (quando presenti):

```bash
cd client && npx --yes ng e2e
```

## UI / Loader

- Loader globale gestito da `LoadingService` e `loadingInterceptor`.
- In `/shop` viene mostrata una progress bar orizzontale sotto l'header durante il caricamento delle card.
- Fuori da `/shop` e' attivo il loader full-screen durante le chiamate HTTP.

## Risorse utili

Per maggiori informazioni su Angular CLI, visita la pagina ufficiale:
[Angular CLI](https://angular.dev/tools/cli)
