# Piano: Refactor Clean Architecture Step 1 Struttura DI

## Data
2026-02-21

## Scope
Refactor incrementale non-breaking del microservizio `Skinet` per introdurre il layer `Application` e separare il wiring DI applicativo/tecnico mantenendo invariati endpoint, route e contratti API.

## Checklist Step
- [x] 1. Discovery tecnico stack e host (Controllers, .NET 9, EF Core, Identity/JWT, Redis, Stripe)
- [x] 2. Mappatura aree a rischio (controller con logica business e validation inline)
- [x] 3. Definizione piano incrementale per rischio/costo
- [x] 4. Creazione progetto `Application` e integrazione in soluzione
- [x] 5. Aggiornamento riferimenti tra layer (`API` e `Infrastructure` verso `Application`)
- [x] 6. Estrazione wiring tecnico da `API` a `Infrastructure.Extensions`
- [x] 7. Introduzione extension method `Application.DependencyInjection`
- [ ] 8. Refactor CQRS endpoint-by-endpoint (Commands/Queries/Handlers)
- [ ] 9. Introduzione test unitari handler CQRS in stile xUnit/FluentAssertions/NSubstitute
- [ ] 10. Hardening logging strutturato con correlation id end-to-end

## Stato finale per step
- 1: completed
- 2: completed
- 3: completed
- 4: completed
- 5: completed
- 6: completed
- 7: completed
- 8: open
- 9: open
- 10: open

## Verifiche eseguite (build/test)
- `dotnet build Skinet.sln`
- `dotnet test Skinet.sln`

## File toccati
- `Application/Application.csproj`
- `Application/DependencyInjection.cs`
- `API/API.csproj`
- `API/Extensions/ServiceCollectionExtensions.cs`
- `Infrastructure/Infrastructure.csproj`
- `Infrastructure/Extensions/InfrastructureServiceCollectionExtensions.cs`
- `Skinet.sln`
