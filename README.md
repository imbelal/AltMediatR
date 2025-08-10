# AltMediatR

AltMediatR is a lightweight, dependency-injection friendly mediator for .NET. The repository contains:
- AltMediatR.Core: request/notification mediator, pre/post processors, and core pipeline behaviors (logging, validation, performance, retry).
- AltMediatR.DDD: optional extensions that add CQRS markers (ICommand/IQuery), query caching, and domain/integration events with a transactional outbox.

## Table of Contents

- Overview
- Features
- Installation
- Quick start
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
- Samples

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

## Features

- Core mediator with true-void support (no Unit exposed in public API).
- Opt‑in pipeline behaviors: Logging, Validation, Performance, Retry (Core).
- Optional DDD features: Commands/Queries markers, Caching (query‑only), Domain/Integration events with outbox (DDD).
- Request pre/post processors.
- Startup validation for duplicate handlers and behavior config.

## Installation

- Requires .NET 8.
- Add the projects to your solution (AltMediatR.Core required; AltMediatR.DDD optional).

## Quick start

```csharp
using AltMediatR.Core.Extensions;
using AltMediatR.DDD.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

var services = new ServiceCollection();
services.AddLogging();
services.AddMemoryCache();

// Core mediator + core behaviors
services.AddAltMediator(s =>
{
    s.AddLoggingBehavior()
     .AddValidationBehavior()
     .AddPerformanceBehavior()
     .AddRetryBehavior();
});

// DDD layer (optional)
services.AddAltMediatorDdd();
services.AddCachingForQueries(o => { o.KeyPrefix = "app:"; });
services.AddTransactionalOutboxBehavior();

// Register handlers by assembly scan (auto-registers handlers + pre/post processors)
services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Optional: behavior ordering
services.AddSingleton(new AltMediatR.Core.Configurations.PipelineConfig
{
    BehaviorsInOrder =
    {
        typeof(AltMediatR.Core.Behaviors.LoggingBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.ValidationBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.PerformanceBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.RetryBehavior<,>),
        typeof(AltMediatR.DDD.Behaviors.CachingBehavior<,>)
    }
});

// Validate configuration (fail fast)
services.ValidateAltMediatorConfiguration(validateBehaviors: true);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<AltMediatR.Core.Abstractions.IMediator>();
```

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
  - Domain events implement `AltMediatR.DDD.Abstractions.IDomainEvent` (also handled via `INotificationHandler<T>`).
  - Integration events implement `AltMediatR.DDD.Abstractions.IIntegrationEvent`.
  - Register `services.AddTransactionalOutboxBehavior()` and enqueue domain/integration events inside your handlers via the scoped queues (`IDomainEventQueue`, `IIntegrationEventQueue`). The transactional behavior will publish domain events (via mediator.PublishAsync) and publish or outbox integration events.

See the Samples project for concrete usage.

## Pipeline behaviors

### Registering behaviors

Use the helpers in `AltMediatR.Core.Extensions.MediatorExtensions` (Core) and `AltMediatR.DDD.Extensions.DddExtensions` (DDD):

- Core: `AddLoggingBehavior()`, `AddValidationBehavior()`, `AddPerformanceBehavior()`, `AddRetryBehavior()`
- DDD: `AddCachingForQueries([options])`, `AddTransactionalOutboxBehavior()`

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
  - TransactionalEventDispatcherBehavior: wraps handler in a transaction, dispatches domain events, and publishes integration events; on publish failure, stores events in `IIntegrationOutbox`.

## Pre/Post processors

- Pre: `IRequestPreProcessor<TRequest>` runs before the handler.
- Post: `IRequestPostProcessor<TRequest,TResponse>` runs after the handler.

They are discovered automatically when you call `RegisterHandlersFromAssembly(...)` on the services collection. You can also register them manually as open generics if you prefer.

## Startup validation

Call `services.ValidateAltMediatorConfiguration(validateBehaviors: true)` after registrations to fail fast on:

- Multiple handlers for the same request type (generic and void).
- Duplicate behavior registrations.
- Missing behaviors listed in `PipelineConfig.BehaviorsInOrder`.

## Samples

See the `AltMediatR.Samples` project for:

- DI setup, handler registration, and behavior configuration (Core + DDD).
- Example command/query/void handlers and pre/post processors.
- Domain/integration events with a console publisher and in-memory outbox.
