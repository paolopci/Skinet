# Verifica Identity Client

## Build client
- Comando: `npm run build --prefix client`
- Risultato: OK (warning budget ~900 KB)

## Verifica API (Newman)
- Collection: `Scripts/Skinet-Identity.postman_collection.json`
- Comando: `npx --yes newman run Scripts/Skinet-Identity.postman_collection.json --env-var baseUrl=https://localhost:5001 --reporters cli --insecure`
- Risultato: tutte le richieste OK (14/14) e 0 assertions fallite

## Note
- Alcuni endpoint possono rispondere 401/400 per token o dati non validi: la collection gestisce gli status attesi.
- Se l'API non risponde, avviare con `dotnet run --project API/API.csproj` prima del test.
