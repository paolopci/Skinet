Macro step in corso: Step 7 - Verifiche e documentazione operativa

- Step 1 - Allineamento contratti Identity e requisiti client (COMPLETATO)
    1. Raccogli l’elenco completo degli endpoint Identity (register, login, refresh, logout, current-user) e relative route
    2. Definisci DTO client coerenti con le response del backend (token, user, errors, validation problems)
    3. Mappa le regole di validazione (required, lunghezze, password policy, email) da backend a client
    4. Definisci comportamento per errori 400/401/403/500 in continuita con l’error interceptor esistente
    5. Verifica la strategia di autenticazione (token in header vs cookie) e impatto sul client

- Step 2 - Fondazioni Auth state e servizi core (COMPLETATO)
    1. Crea un AuthService in `core/services` con metodi per register/login/logout/refresh/current-user
    2. Definisci un modello di stato autenticazione (es. `AuthState`) e usa signals per user e status
    3. Implementa una strategia di persistenza token/sessione (LocalStorage o cookie httpOnly) senza cambiare architettura
    4. Aggiungi un auth interceptor per allegare il token alle richieste protette
    5. Inserisci un guard per route protette (es. checkout) e una redirect policy post-login

- Step 3 - Routing e wiring delle feature Identity (COMPLETATO)
    1. Introduci un feature module/area `features/account` con route `login` e `register`
    2. Configura le rotte in `app.routes.ts` mantenendo la struttura esistente
    3. Aggiungi fallback di navigazione in caso di 401 per riportare al login
    4. Integra il recupero `current-user` on app start per ripristino sessione

- Step 4 - UI Login e Registrazione (COMPLETATO)
    1. Crea componenti `login` e `register` usando Reactive Forms e pattern UI esistenti
    2. Implementa validazioni client e messaggi di errore con `api-error` utilities
    3. Integra snackbar/feedback visivo coerente con Material
    4. Adatta layout per mobile e UX minimale senza modifiche invasive

- Step 5 - Gestione errori e UX di sessione (COMPLETATO)
    1. Allinea le validation errors del backend alle form con mapping dedicato
    2. Gestisci token scaduti con refresh e messaggio coerente
    3. Mostra stato di autenticazione nel header (login/logout) con signals
    4. Aggiungi schermate/CTA per accesso negato e sessione scaduta

- Step 6 - Sicurezza e configurazione ambiente (COMPLETATO)
    1. Centralizza gli endpoint API in environment e evita URL hardcoded
    2. Se usi cookie, gestisci CORS e `withCredentials`
    3. Definisci policy di logout (server + client) e pulizia dati sensibili
    4. Controlla le dipendenze e aggiorna configurazioni minime per Angular 21

- Step 7 - Verifiche e documentazione operativa (COMPLETATO)
    1. Aggiungi test minimi per AuthService e guard (quando presenti)
    2. Verifica flussi: register, login, refresh, logout, accesso route protette (COMPLETATO: Newman)
    3. Documenta i flussi e gli endpoint usati in un breve README operativo (COMPLETATO)
    4. Valida che loader e snackbar non si sovrappongano ai flussi di login