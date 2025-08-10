# AltMediatR

AltMediatR is a lightweight, dependency-injection friendly mediator for .NET. The repository contains:

- AltMediatR.Core: request/notification mediator, pre/post processors, and core pipeline behaviors (logging, validation, performance, retry).
- AltMediatR.DDD: optional extensions that add CQRS markers (ICommand/IQuery), query caching, and domain/integration events with a transactional outbox.
- AltMediatR.WebApiSample: minimal ASP.NET Core Web API demonstrating Core + DDD with EF Core InMemory, aggregates, domain/integration events, and in-memory publisher/outbox.

## Table of Contents

- Overview
- Features
- Installation
- Quick start (Web API)
- Requests and handlers
  - Commands (with/without response)
  - Queries (with caching)
  - Notifications and events
- Pipeline behaviors
  - Registering behaviors
  - Behavior ordering
  - Built-in behaviors (Core vs DDD)
- Pre/Post processors
- Startup validation
- Sample Web API

## Overview

AltMediatR promotes decoupled communication via the Mediator pattern.

- Core (AltMediatR.Core)
  - Requests: `IRequest<TResponse>` and `IRequest` (true void).
  - Handlers: `IRequestHandler<TRequest,TResponse>`, `IRequestHandler<TRequest>`, `INotificationHandler<TNotification>`.
  - Pipelines: Add only the behaviors you need, in the order you choose.
- DDD (AltMediatR.DDD)
  - CQRS markers: `ICommand<TResponse>`, `IQuery<TResponse>`.
  - Query caching via `IMemoryCache` with `ICacheable` and `CachingOptions`.
  - Domain events (`IDomainEvent`) and integration events (`IIntegrationEvent`) with a transactional outbox behavior.
  - Aggregates raise events via `AggregateRootBase`; events are collected with `IEventQueueCollector` (EF Core or in-memory).

## Features

- Core mediator with true-void support (no Unit exposed in public API).
- Opt‑in pipeline behaviors: Logging, Validation, Performance, Retry (Core).
- Optional DDD features: Commands/Queries markers, Caching (query‑only), Domain/Integration events with outbox (DDD).
- Aggregate-driven events via `AggregateRootBase` + `IEventQueueCollector` (no request-scoped queues).
- Request pre/post processors.
- Startup validation for duplicate handlers and behavior config.

## Installation

- Requires .NET 8.
- Add the projects to your solution (AltMediatR.Core required; AltMediatR.DDD optional).

## Quick start (Web API)

Program.cs setup using EF Core InMemory, Core + DDD, Swagger, and handler scanning:

```csharp
using AltMediatR.Core.Extensions;
using AltMediatR.DDD.Extensions;
using AltMediatR.DDD.Abstractions;
using AltMediatR.WebApiSample.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.CustomSchemaIds(t => t.FullName));

builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("app-db"));

builder.Services.AddAltMediator(s =>
{
    s.AddLoggingBehavior()
     .AddValidationBehavior()
     .AddPerformanceBehavior()
     .AddRetryBehavior();
});

builder.Services.AddAltMediatorDdd()
                .AddTransactionalOutboxBehavior()
                .AddInMemoryIntegrationEventPublisher()
                .AddInMemoryOutboxStore();

builder.Services.AddCachingForQueries(_ => { });

builder.Services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<IEventQueueCollector, EfChangeTrackerEventCollector>();
builder.Services.AddScoped<ITransactionManager, EfTransactionManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

Controllers (example):

```csharp
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public Task<string> Create(CreateUserRequest req, CancellationToken ct)
        => _mediator.SendAsync(new CreateUserCommand(req.Name), ct);

    [HttpGet]
    public Task<IReadOnlyList<UserDto>> GetAll(CancellationToken ct)
        => _mediator.SendAsync(new GetUsersQuery(), ct);
}
```

Aggregates raise events via `AggregateRootBase` and are tracked by EF’s ChangeTracker; the transactional behavior publishes domain events (via mediator) and publishes or outboxes integration events.

## Requests and handlers

### Commands (with response)

Use the DDD command marker:

```csharp
using AltMediatR.DDD.Abstractions;

public sealed class CreateUserCommand : ICommand<string>
{
    public required string Name { get; init; }
}

public sealed class CreateUserHandler : AltMediatR.Core.Abstractions.IRequestHandler<CreateUserCommand, string>
{
    public Task<string> HandleAsync(CreateUserCommand request, CancellationToken ct)
        => Task.FromResult(Guid.NewGuid().ToString());
}

var id = await mediator.SendAsync(new CreateUserCommand { Name = "Jane" });
```

### Commands (void)

Use the core void request for commands without a response:

```csharp
using AltMediatR.Core.Abstractions;

public sealed class DeleteUserCommand : IRequest
{
    public required string UserId { get; init; }
}

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    public Task HandleAsync(DeleteUserCommand request, CancellationToken ct)
        => Task.CompletedTask;
}

await mediator.SendAsync(new DeleteUserCommand { UserId = id });
```

### Queries (with caching)

Use the DDD query marker and ICacheable:

```csharp
using AltMediatR.DDD.Abstractions;

public sealed class GetUserQuery : IQuery<string>, ICacheable
{
    public required string UserId { get; init; }
    public string CacheKey => $"user:{UserId}";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(5);
}

public sealed class GetUserHandler : AltMediatR.Core.Abstractions.IRequestHandler<GetUserQuery, string>
{
    public Task<string> HandleAsync(GetUserQuery request, CancellationToken ct)
        => Task.FromResult($"User: {request.UserId}");
}

var user = await mediator.SendAsync(new GetUserQuery { UserId = id });
```

Note: enable caching with `services.AddCachingForQueries(...)` from `AltMediatR.DDD.Extensions`.

### Notifications and events

Notifications use the core `INotification` and `INotificationHandler<T>`. Domain/integration events are part of DDD.

- Simple notification:

```csharp
using AltMediatR.Core.Abstractions;

public sealed class UserCreatedNotification : INotification
{
    public required string UserId { get; init; }
}

public sealed class UserCreatedNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification e, CancellationToken ct) => Task.CompletedTask;
}

await mediator.PublishAsync(new UserCreatedNotification { UserId = id });
```

- DDD domain/integration events:
  - Domain events implement `AltMediatR.DDD.Abstractions.IDomainEvent` and handlers can implement `IDomainEventHandler<T>`.
  - Integration events implement `AltMediatR.DDD.Abstractions.IIntegrationEvent` and (optionally) `IIntegrationEventHandler<T>` for in-process handling.
  - Register `services.AddTransactionalOutboxBehavior()` and raise events from aggregates (`AggregateRootBase`). The transactional behavior collects events, publishes domain events (via mediator), and publishes or outboxes integration events.

## Pipeline behaviors

### Registering behaviors

Use the helpers in `AltMediatR.Core.Extensions.MediatorExtensions` (Core) and `AltMediatR.DDD.Extensions.DddExtensions` (DDD):

- Core: `AddLoggingBehavior()`, `AddValidationBehavior()`, `AddPerformanceBehavior()`, `AddRetryBehavior()`
- DDD: `AddCachingForQueries([options])`, `AddTransactionalOutboxBehavior()`, `AddInMemoryOutboxStore()`, `AddInMemoryIntegrationEventPublisher()`

### Behavior ordering

Provide a `PipelineConfig` singleton with `BehaviorsInOrder` listing open generic behavior types in desired order. Behaviors not listed run afterward.

### Built-in behaviors (Core vs DDD)

- Core
  - LoggingBehavior: logs before/after handler.
  - ValidationBehavior: uses `IValidator<TRequest>` (default `NoOpValidator` registered).
  - PerformanceBehavior: times handler execution.
  - RetryBehavior: simple retry with logging on exceptions.
- DDD
  - CachingBehavior: caches only `IQuery<T>` requests implementing `ICacheable`.
  - TransactionalEventDispatcherBehavior: wraps handler in a transaction, dispatches domain events (via mediator PublishAsync), and publishes integration events; on publish failure, stores events in `IOutboxStore`.

## Pre/Post processors

- Pre: `IRequestPreProcessor<TRequest>` runs before the handler.
- Post: `IRequestPostProcessor<TRequest,TResponse>` runs after the handler.

They are discovered automatically when you call `RegisterHandlersFromAssembly(...)` on the services collection. You can also register them manually as open generics if you prefer.

## Startup validation

Call `services.ValidateAltMediatorConfiguration(validateBehaviors: true)` after registrations to fail fast on:

- Multiple handlers for the same request type (generic and void).
- Duplicate behavior registrations.
- Missing behaviors listed in `PipelineConfig.BehaviorsInOrder`.

## Sample Web API

This repo includes `AltMediatR.WebApiSample`, showing:

- DI setup for Core + DDD with EF Core InMemory
- Aggregates (User, Order) raising domain/integration events
- EF ChangeTracker-based event collection
- Transactional outbox behavior with in-memory publisher/outbox
- Query caching for list endpoints
- Swagger UI

Run locally:

```powershell
# from repo root
 dotnet run --project .\AltMediatR.WebApiSample\AltMediatR.WebApiSample.csproj
# Swagger UI
 start http://localhost:5152/swagger
```
