# Coding Conventions

This file defines coding standards, folder structure, patterns, and logging rules for all agents in the repository.

Agents must follow these conventions to ensure **consistency, Clean Architecture, security, and multi-agent collaboration**.

---

## General Naming

- **Classes**: PascalCase (e.g., `PaymentService`, `PaymentController`)
- **Interfaces**: `I` + PascalCase (e.g., `IPaymentService`)
- **Methods**: PascalCase + `Async` suffix for async methods (e.g., `CreatePaymentAsync`)
- **Variables / parameters**: camelCase (e.g., `paymentRequest`)
- **Constants**: UPPER_CASE_WITH_UNDERSCORES
- **Folder names**: PascalCase or the existing repository convention for shared folders (e.g., `Services`, `Controllers`, `Dtos`, `Interfaces`)

---

## Project Folder Structure

Agents must follow this structure:

```text
Nexus/
  Nexus.Api/
    Controllers/
    Extensions/
    Middleware/
  Nexus.Application/
    Common/
    Constants/
    Dtos/
    Enums/
    Exceptions/
    Extensions/
    Factories/
    Interfaces/
    Services/
    Settings/
  Nexus.Domain/
    Entities/
    Enums/
  Nexus.Infrastructure/
    Interfaces/
    Migrations/
    Persistence/
    ReadModels/
    Repositories/
Nexus.Network/
    Constants/
    Dtos/
    Enums/
    Exceptions/
    Extensions/
    Interfaces/
    Responses/
    Services/
Nexus.Tests/
    Api/
    Application/
    Network/
```

---

## SOLID & OOP Principles

- Single Responsibility
- Open/Closed
- Liskov Substitution
- Interface Segregation
- Dependency Inversion
- Use **Dependency Injection** for all dependencies
- Keep **business logic in Application / Domain layers**
- Avoid **direct DB calls in API layer**
- Keep controllers thin and orchestration-focused only

---

## EF Core & Database Conventions

- Always use **async methods** (`ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`)
- Inject `DbContext` via **constructor DI**
- Use **migrations** to update schema
- Add indexes for frequently queried columns (e.g., `PaymentId`, `CustomerId`)
- Avoid N+1 queries

---

## Repository & Unit of Work Patterns

- Repository pattern encapsulates EF Core DbSets:

```csharp
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Payment entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment entity, CancellationToken cancellationToken = default);
}
```

- Unit of Work pattern manages transactions across multiple repositories:
- Use `IUnitOfWork.SaveChanges` to commit changes

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChanges();
}
```

- Place repositories in `Infrastructure/Repositories`

---

## API And Validation Reference

- Follow `Docs/Shared/ApiConventions.md` for controller, endpoint, response, DTO, and validation rules
- Keep `CodingConventions.md` focused on naming, structure, persistence, and testing guidance

---

## Unit Testing Conventions

- Use **xUnit** for tests
- Use **Moq** to mock repositories and services
- Test **services, controllers, and interfaces**
- Test edge cases and error handling
- Naming: `Method_Should_Result_When_Condition`
  - Example: `CreatePayment_Should_ReturnSuccess_When_RequestIsValid`
  - Example: `Post_Should_ReturnBadRequest_When_RequestIsInvalid`

---

## Summary

Agents must:

1. Follow **naming conventions**
2. Follow **folder structure**
3. Apply **SOLID/OOP principles**
4. Use **async EF Core methods**
5. Follow `Docs/Shared/ApiConventions.md` for DTO and validation rules
6. Use **ILogger and proper error handling**
7. Implement required API controllers/endpoints
8. Write **xUnit/Moq tests** for services, and controllers

This ensures **clean, consistent, secure, and API-complete code** across all agents.
