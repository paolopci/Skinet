# Piano Test Checkout Address

## Obiettivo
Verificare il flusso checkout Address con mapping internazionale verso API account address e comportamento della checkbox "Save as default address".

## Prerequisiti
- Backend avviato e autenticazione funzionante.
- Client avviato con `npm run start --prefix client`.
- Utente autenticato con carrello valido.

## Matrice Paesi
- IT: `region` obbligatoria.
- US: `region` obbligatoria.
- GB: `region` opzionale.

## Casi con checkbox OFF
1. Compilare Address valido (IT), lasciare checkbox OFF, cliccare Next.
2. Atteso: passaggio a Shipping senza chiamata PUT `/api/account/address`.
3. Tornare da Shipping a Address con Back.
4. Atteso: reload form da GET `/api/account/address`.

## Casi con checkbox ON
1. Compilare Address valido (IT) con region valorizzata, checkbox ON, cliccare Next.
2. Atteso: chiamata PUT `/api/account/address` con payload internazionale.
3. Atteso: snackbar di conferma e passaggio a Shipping.
4. Tornare a Address da Shipping.
5. Atteso: valori ripopolati dal backend (GET `/api/account/address`).

## Validazioni specifiche paese
1. IT con region vuota e checkbox ON.
2. Atteso: blocco step Address + messaggio di warning su region obbligatoria.
3. US con region vuota e checkbox ON.
4. Atteso: blocco step Address + messaggio di warning su region obbligatoria.
5. GB con region vuota e checkbox ON.
6. Atteso: salvataggio consentito e passaggio a Shipping.

## Validazioni campi obbligatori
1. Lasciare line1/city/postalCode vuoti.
2. Atteso: blocco su Address prima di avanzare.
3. Impostare country non supportato.
4. Atteso: blocco salvataggio con warning "Paese non supportato. Usa IT, US o GB.".

## Error handling API
1. Simulare 401 su PUT address.
2. Atteso: messaggio sessione scaduta, nessun avanzamento step.
3. Simulare 400 con validation errors.
4. Atteso: visualizzazione primo errore validazione backend, nessun avanzamento.
5. Simulare errore rete/500.
6. Atteso: messaggio generico errore salvataggio, nessun avanzamento.

## Comandi verifica
- Build: `npm run build --prefix client`
- Smoke runtime: `npm run start --prefix client`

## Esito atteso finale
- Mapping payload coerente con DTO backend.
- Flusso Next stabile con checkbox ON/OFF.
- Ritorno Shipping -> Address con ricarica da backend.
