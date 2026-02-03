# Scripts

## identity-reset.sql
Script SQL per azzerare solo le tabelle Identity (AspNet*) senza toccare dati di dominio come `products`.

### Uso rapido (SQL Server)
1) Seleziona il database di test (es. `SkinetDB_Test`).
2) Esegui lo script `identity-reset.sql`.

### Note
- Cancella utenti, ruoli, claims, logins e tokens.
- Usare solo in ambienti di test/sviluppo.
