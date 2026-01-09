---
description: 'Build C# applications using Clean Architecture principles with DDD, FluentResults, and use case boundary patterns'
---

# Clean Architecture for .NET

## Core Principles

| Principle | Description |
|-----------|-------------|
| Dependency Inversion | Dependencies point inward toward the Domain layer |
| Independence | Business logic is independent of frameworks, UI, and external concerns |
| Testability | Business logic can be tested without UI, database, or external services |
| Layer Separation | Clear boundaries between Domain, Application, Infrastructure, and Presentation |

## Dependency Rules

| Direction | Allowed |
|-----------|---------|
| Infrastructure → Core | ✅ |
| Presentation → Core + Bootstrapper | ✅ |
| Tests → Core | ✅ |
| Bootstrapper → Core + Infrastructure | ✅ |
| Core → Infrastructure | ❌ |
| Core → Presentation | ❌ |
| Core → Bootstrapper | ❌ |

## Project Structure

```
{AppName}/
├── {AppName}.sln
├── {AppName}.Bootstrapper/                     # DI configuration
│   └── {AppName}.Bootstrapper.csproj          # → Core + Infrastructure
├── {AppName}.Core/                             # Domain + Application layers
│   ├── {AppName}.Core.csproj
│   ├── Domain/
│   │   └── {AggregateRoot}/                   # One folder per aggregate root
│   ├── ExternalInterfaces/                     # Repository and service contracts
│   └── UseCases/
│       └── IUseCaseInteractor.cs
├── {AppName}.Core.UnitTests/                   # → Core only
│   ├── Domain/
│   └── UseCases/
├── {AppName}.Infrastructure.{Provider}/        # e.g., EntityFramework, OpenAI
│   └── {AppName}.Infrastructure.{Provider}.csproj  # → Core only
├── {AppName}.Infrastructure.IntegrationTests/
└── {AppName}.WebApi/                           # Presentation layer
    └── {AppName}.WebApi.csproj                # → Core + Bootstrapper
```

## Project Setup Commands

```pwsh
# Create solution and projects
dotnet new sln -n {AppName}
dotnet new classlib -n {AppName}.Core
dotnet new classlib -n {AppName}.Infrastructure  
dotnet new classlib -n {AppName}.Bootstrapper
dotnet new webapi -minimal -n {AppName}.WebApi
dotnet new xunit -n {AppName}.Core.UnitTests
dotnet new xunit -n {AppName}.Infrastructure.IntegrationTests
dotnet sln add **/*.csproj

# Set up references (arrows show dependency direction)
dotnet add {AppName}.Bootstrapper reference {AppName}.Core {AppName}.Infrastructure
dotnet add {AppName}.WebApi reference {AppName}.Core {AppName}.Bootstrapper
dotnet add {AppName}.Infrastructure reference {AppName}.Core
dotnet add {AppName}.Core.UnitTests reference {AppName}.Core
dotnet add {AppName}.Infrastructure.IntegrationTests reference {AppName}.Infrastructure

# Required packages
dotnet add {AppName}.Core package FluentResults
dotnet add {AppName}.Core.UnitTests package FluentAssertions
dotnet add {AppName}.Core.UnitTests package Moq

# IMPORTANT: Delete generated placeholder files
Remove-Item {AppName}.Core/Class1.cs, {AppName}.Infrastructure/Class1.cs, {AppName}.Bootstrapper/Class1.cs -ErrorAction SilentlyContinue
Remove-Item {AppName}.Core.UnitTests/UnitTest1.cs, {AppName}.Infrastructure.IntegrationTests/UnitTest1.cs -ErrorAction SilentlyContinue
```

## Domain Layer

The Domain layer is the innermost layer containing business logic and domain models. It has **zero external dependencies**.

**Requirements**:
- Use DDD principles for aggregates, entities, and value objects
- Use FluentResults `Result<T>` for all factory methods
- Place aggregate roots and their children in dedicated folders
- Entities use classes; value objects use records

### Entities (classes)
```csharp
public class SomeEntity
{
    private SomeEntity(string value) => Value = value;
    
    public string Value { get; private set; }
    
    public static Result<SomeEntity> TryCreate(string someValue)
    {
        if (string.IsNullOrWhiteSpace(someValue))
            return Result.Fail<SomeEntity>("Some value cannot be null or empty.");

        return Result.Ok(new SomeEntity(someValue));
    }
}
```

### Value Objects (records)

```csharp
public record EmailAddress
{
    private EmailAddress(string value) => Value = value;
    
    public string Value { get; init; }
    
    public static Result<EmailAddress> TryCreate(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return Result.Fail<EmailAddress>("Invalid email format.");
            
        return Result.Ok(new EmailAddress(email));
    }
    
    private static bool IsValidEmail(string email) => 
        email.Contains('@') && email.Contains('.');
}
```

## Application Layer

The Application layer orchestrates use case flows. It resides in the **Core** project alongside the Domain layer.

### IUseCaseInteractor Interface

```csharp
/// <summary>
/// Use case interactor interface.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TOutput">Output type.</typeparam>
public interface IUseCaseInteractor<TRequest, TOutput>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <returns>Output.</returns>
    Task<TOutput> HandleAsync(TRequest request);
}
```

### Use Case Folder Structure

Each use case gets its own folder with exactly **4 files**:

```
UseCases/
└── {UseCaseName}/
    ├── {UseCaseName}Request.cs      # Input DTO
    ├── {UseCaseName}Response.cs     # Output DTO
    ├── I{UseCaseName}Boundary.cs    # Output port interface
    └── {UseCaseName}Interactor.cs   # Use case implementation
```

### Boundary Interface Pattern

Boundary interfaces define outcome methods for each use case event. They are generic on `TOutput` to decouple from presentation-layer types.

```csharp
public interface ICreateUserBoundary<TOutput>
{
    TOutput UserCreated(CreateUserResponse response, TOutput output);
    TOutput UserAlreadyExists(CreateUserResponse response, TOutput output);
    TOutput ValidationFailed(CreateUserResponse response, TOutput output);
}
```

### Complete Use Case Example
```csharp
public class CreateUserInteractor : IUseCaseInteractor<CreateUserRequest, Task>
{
    private readonly IUserRepository _userRepository;
    private readonly ICreateUserBoundary<Task> _boundary;

    public CreateUserInteractor(IUserRepository userRepository, ICreateUserBoundary<Task> boundary)
    {
        _userRepository = userRepository;
        _boundary = boundary;
    }

    public async Task<Task> HandleAsync(CreateUserRequest request)
    {
        var emailResult = EmailAddress.TryCreate(request.Email);
        if (emailResult.IsFailed)
            return _boundary.ValidationFailed(new CreateUserResponse(emailResult.Errors.First().Message), Task.CompletedTask);

        var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value);
        if (existingUser != null)
            return _boundary.UserAlreadyExists(new CreateUserResponse("User already exists"), Task.CompletedTask);

        var userResult = User.TryCreate(request.Name, emailResult.Value);
        if (userResult.IsFailed)
            return _boundary.ValidationFailed(new CreateUserResponse(userResult.Errors.First().Message), Task.CompletedTask);

        await _userRepository.SaveAsync(userResult.Value);
        return _boundary.UserCreated(new CreateUserResponse("User created successfully"), Task.CompletedTask);
    }
}
```
### External Interfaces

Define contracts in `Core/ExternalInterfaces/` for Infrastructure to implement:

```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(EmailAddress email);
    Task<User?> GetByIdAsync(UserId id);
    Task SaveAsync(User user);
    Task DeleteAsync(UserId id);
}
```

## Infrastructure Layer

The Infrastructure layer implements external interfaces from Core. Use separate projects per provider (e.g., `{AppName}.Infrastructure.EntityFramework`, `{AppName}.Infrastructure.SemanticKernel`).

### Repository Implementation
```csharp
public class UserRepository : IUserRepository
{
    private readonly DbContext _context;

    public UserRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(EmailAddress email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value);
    }

    public async Task SaveAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}
```

## Presentation Layer

The Presentation layer handles user/client interactions. Use ASP.NET Core minimal APIs for Web APIs.

**Presenter placement**: Place presenters in folders matching their use cases for discoverability.

### Presenter Implementation
```csharp
public class CreateUserPresenter : ICreateUserBoundary<IResult>
{
    public IResult UserCreated(CreateUserResponse response, IResult output)
    {
        return Results.Created("/users/{id}", new { Message = response.Message });
    }

    public IResult UserAlreadyExists(CreateUserResponse response, IResult output)
    {
        return Results.Conflict(new { Error = response.Message });
    }

    public IResult ValidationFailed(CreateUserResponse response, IResult output)
    {
        return Results.BadRequest(new { Error = response.Message });
    }
}
```

### Minimal API Endpoint

```csharp
app.MapPost("/users", async (
    CreateUserRequest request, 
    IUseCaseInteractor<CreateUserRequest, IResult> interactor) =>
{
    return await interactor.HandleAsync(request);
});
```

## Bootstrapper

The Bootstrapper wires up DI without referencing presentation-layer types (like `IResult`). Use one of these approaches:

### Approach 1: Core Services Only (Recommended)

The Bootstrapper registers only Core and Infrastructure services. The presentation layer handles its own presenter registrations.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAppCoreServices(this IServiceCollection services)
    {
        // Register domain services and repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Register use case interactors (without output type binding)
        services.AddScoped<CreateUserInteractor>();
        services.AddScoped<GetUserInteractor>();
        
        return services;
    }
}
```

Then in the presentation layer (`Program.cs` for Web API):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register core services
builder.Services.AddMyAppCoreServices();

// Register presentation-specific services
builder.Services.AddScoped<IUseCaseInteractor<CreateUserRequest, Task>, CreateUserInteractor>();
builder.Services.AddScoped<ICreateUserBoundary<IResult>, CreateUserPresenter>();

var app = builder.Build();
```

### Approach 2: Generic Registration Extension

The Bootstrapper provides a method that the presentation layer calls with its specific types:

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAppServices(
      this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<IUserRepository, UserRepository>();
        
        return services;
    }
    
    public static IServiceCollection AddUseCase<TRequest, TOutput, TInteractor>(
        this IServiceCollection services)
        where TInteractor : class, IUseCaseInteractor<TRequest, TOutput>
    {
        services.AddScoped<
          IUseCaseInteractor<TRequest, TOutput>, TInteractor>();
          
        return services;
    }
    
    public static IServiceCollection AddBoundary<TBoundary, TOutput, TImplementation>(
        this IServiceCollection services)
        where TImplementation : class, TBoundary
        where TBoundary : class
    {
        services.AddScoped<TBoundary, TImplementation>();
        return services;
    }
}
```

Usage in presentation layer:
```csharp
builder.Services.AddMyAppServices()
    .AddUseCase<CreateUserRequest, Task, CreateUserInteractor>()
    .AddBoundary<ICreateUserBoundary<IResult>, IResult, CreateUserPresenter>();
```

**Recommendation**: Use **Approach 1** for most applications as it maintains clear separation of concerns and is easiest to understand and maintain.

## Testing

| Package | Purpose |
|---------|----------|
| xUnit | Test framework |
| Moq | Mocking external interfaces |
| FluentAssertions | Readable assertions |

### TDD Workflow

1. Write failing unit test
2. Implement minimum code to pass
3. Refactor while keeping tests green
4. **One assertion per test method**

### Test Examples
```csharp
[Fact]
public void TryCreate_WithValidValue_ShouldReturnSuccess()
{
    // Arrange
    var validValue = "test@example.com";
    
    // Act
    var result = EmailAddress.TryCreate(validValue);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
}

[Fact]
public async Task HandleAsync_WithValidRequest_ShouldCallUserCreated()
{
    // Arrange
    var request = new CreateUserRequest("John", "john@example.com");
    var mockRepository = new Mock<IUserRepository>();
    var mockBoundary = new Mock<ICreateUserBoundary<Task>>();
    var interactor = new CreateUserInteractor(
      mockRepository.Object, mockBoundary.Object);
    
    // Act
    await interactor.HandleAsync(request);
    
    // Assert
    mockBoundary.Verify(
      b => b.UserCreated(
        It.IsAny<CreateUserResponse>(), It.IsAny<Task>()), Times.Once);
}
```

**Run tests after each implementation step.** Save all files before running tests.

## Validation Checklist

| Check | Requirement |
|-------|-------------|
| Dependencies | All point inward toward Core |
| FluentResults | Used for all domain `TryCreate` with `Result<T>` |
| Use Case Files | Exactly 4 per use case: Request, Response, Boundary, Interactor |
| Project References | Core has no references to Infrastructure or Presentation |
| Testing | All tests pass; written before implementation (TDD) |
| Domain Logic | No external dependencies in Domain layer |
| Aggregates | Related entities grouped under aggregate root folders |
| Bootstrapper | No presentation-layer types registered |
| Cleanup | Deleted generated `Class1.cs` and `UnitTest1.cs` files |

## Anti-Patterns

| ❌ Avoid | ✅ Instead |
|----------|----------|
| Core → Infrastructure reference | Infrastructure implements Core interfaces |
| Business logic in presentation | Move to use case interactor |
| Skipping FluentResults | Use `Result<T>` for domain factory methods |
| Multiple assertions per test | One assertion per test method |
| Direct DB calls from use cases | Use repository interfaces |
| Presentation types in Bootstrapper | Register in presentation layer |

## Quick Reference

| Layer | Project | References | Contains |
|-------|---------|------------|----------|
| Domain | Core | (none) | Entities, Value Objects, Aggregates |
| Application | Core | (none) | Use Cases, External Interfaces |
| Infrastructure | Infrastructure.* | Core | Repository/Service implementations |
| Presentation | WebApi | Core + Bootstrapper | Endpoints, Presenters |
| Composition | Bootstrapper | Core + Infrastructure | DI registration |