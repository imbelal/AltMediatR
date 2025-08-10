# AltMediatR

AltMediatR is a lightweight, dependency-injection friendly mediator for .NET. It supports commands, queries, notifications, pre/post processors, and opt‑in pipeline behaviors (logging, validation, performance, retry, caching). It also includes first‑class support for domain and integration events with a transactional outbox pattern.

## Table of Contents

- Overview
- Features
- Installation
- Quick start
- Requests and handlers
  - Commands (with/without response)
  - Queries (with caching)
  - Notifications
- Pipeline behaviors
  - Registering behaviors
  - Behavior ordering
  - Built-in behaviors
- Pre/Post processors
- Domain and integration events
  - Transactional outbox
- Startup validation
- Samples

## Overview

AltMediatR promotes decoupled communication via the Mediator pattern.

- Requests: `IRequest<TResponse>` and `IRequest` (true void).
- Handlers: `IRequestHandler<TRequest,TResponse>`, `IRequestHandler<TRequest>`, `INotificationHandler<TNotification>`.
- DI: Built on `Microsoft.Extensions.DependencyInjection`.
- Pipelines: Add only the behaviors you need, in the order you choose.
- Events: In‑process domain events and out‑of‑process integration events with outbox fallback.

## Features

- Commands/Queries via markers: `ICommand<T>`, `ICommand`, `IQuery<T>`.
- True void pipeline (no Unit exposed) for `IRequest` + `IRequestHandler<TRequest>`.
- Opt‑in pipeline behaviors: Logging, Validation, Performance, Retry, Caching (query‑only).
- Request pre/post processors.
- Query caching using `IMemoryCache` with `ICacheable` and `CachingOptions`.
- Domain events (`IDomainEvent`) and integration events (`IIntegrationEvent`).
- Transactional outbox via `ITransactionManager` + `IIntegrationOutbox`.
- Startup validation for duplicate handlers and behavior config.

## Installation

- Requires .NET 8.
- Add the AltMediatR projects to your solution or reference the library.

## Quick start

```csharp
using AltMediatR.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

var services = new ServiceCollection();
services.AddLogging();
services.AddMemoryCache();

services.AddAltMediator(s =>
{
    s.AddLoggingBehavior()
     .AddValidationBehavior()
     .AddPerformanceBehavior()
     .AddRetryBehavior()
     .AddCachingForQueries(o => { o.KeyPrefix = "app:"; });
     // For events + outbox
     s.AddTransactionalOutboxBehavior();
});

// Optional: behavior ordering
services.AddSingleton(new AltMediatR.Core.Configurations.PipelineConfig
{
    BehaviorsInOrder =
    {
        typeof(AltMediatR.Core.Behaviors.LoggingBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.ValidationBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.PerformanceBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.RetryBehavior<,>),
        typeof(AltMediatR.Core.Behaviors.CachingBehavior<,>)
    }
});

// Register handlers by assembly scan
services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Optional pre/post processors
services.RegisterRequestPreProcessor(typeof(MyPreProcessor<>));
services.RegisterRequestPostProcessor(typeof(MyPostProcessor<,>));

// Event infrastructure (implementations shown in Samples)
services.AddSingleton<ITransactionManager, MyTransactionManager>();
services.AddSingleton<IIntegrationEventPublisher, MyIntegrationEventPublisher>();
services.AddSingleton<IIntegrationOutbox, MyIntegrationOutbox>();

// Validate configuration (fail fast)
services.ValidateAltMediatorConfiguration(validateBehaviors: true);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
```

## Requests and handlers

### Commands (with response)

```csharp
public sealed class CreateUserCommand : ICommand<string>
{
    public required string Name { get; init; }
}

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
{
    public Task<string> HandleAsync(CreateUserCommand request, CancellationToken ct)
        => Task.FromResult(Guid.NewGuid().ToString());
}

var id = await mediator.SendAsync(new CreateUserCommand { Name = "Jane" });
```

### Commands (void)

```csharp
public sealed class DeleteUserCommand : ICommand
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

```csharp
public sealed class GetUserQuery : IQuery<string>, ICacheable
{
    public required string UserId { get; init; }
    public string CacheKey => $"user:{UserId}";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(5);
}

public sealed class GetUserHandler : IRequestHandler<GetUserQuery, string>
{
    public Task<string> HandleAsync(GetUserQuery request, CancellationToken ct)
        => Task.FromResult($"User: {request.UserId}");
}

var user = await mediator.SendAsync(new GetUserQuery { UserId = id });
```

### Notifications

```csharp
public sealed class UserCreatedDomainEvent : IDomainEvent
{
    public required string UserId { get; init; }
}

public sealed class UserCreatedHandler : INotificationHandler<UserCreatedDomainEvent>
{
    public Task HandleAsync(UserCreatedDomainEvent e, CancellationToken ct)
        => Task.CompletedTask;
}

await mediator.PublishDomainEventAsync(new UserCreatedDomainEvent { UserId = id });
```

## Pipeline behaviors

### Registering behaviors

Use the helpers in `MediatorExtensions`:

- `AddLoggingBehavior()`
- `AddValidationBehavior()`
- `AddPerformanceBehavior()`
- `AddRetryBehavior()`
- `AddCachingForQueries([options])`
- `AddTransactionalOutboxBehavior()` (dispatches domain events; publishes integration events with outbox fallback)

### Behavior ordering

Provide a `PipelineConfig` singleton with `BehaviorsInOrder` listing open generic behavior types in desired order. Behaviors not listed run afterward.

### Built-in behaviors

- LoggingBehavior: logs before/after handler.
- ValidationBehavior: uses `IValidator<TRequest>` (default `NoOpValidator` registered).
- PerformanceBehavior: times handler execution.
- RetryBehavior: simple retry with logging on exceptions.
- CachingBehavior: caches only `IQuery<T>` requests implementing `ICacheable`.
- Transactional outbox behavior: wraps handler in a transaction, dispatches domain events, and publishes integration events; on publish failure, stores events in `IIntegrationOutbox`.

## Pre/Post processors

- Pre: `IRequestPreProcessor<TRequest>` runs before the handler.
- Post: `IRequestPostProcessor<TRequest,TResponse>` runs after the handler.
  Register with `RegisterRequestPreProcessor` and `RegisterRequestPostProcessor`.

## Domain and integration events

- Domain events: implement `IDomainEvent` and handle via `INotificationHandler<TDomainEvent>`.
- Integration events: implement `IIntegrationEvent` and publish via `IIntegrationEventPublisher`.
- Use `AddTransactionalOutboxBehavior()` to ensure:
  - Domain events dispatch within the same transaction.
  - Integration events are published; if publishing fails, they are stored in `IIntegrationOutbox` for later delivery.

Infrastructure abstractions:

- `ITransactionManager` provides `BeginAsync()` creating a transactional scope.
- `IIntegrationEventPublisher` sends integration events to your transport (e.g., bus).
- `IIntegrationOutbox` persists events if publish fails (sample in-memory outbox included).

## Startup validation

Call `services.ValidateAltMediatorConfiguration(validateBehaviors: true)` after registrations to fail fast on:

- Multiple handlers for the same request type (generic and void).
- Duplicate behavior registrations.
- Missing behaviors listed in `PipelineConfig.BehaviorsInOrder`.

## Samples

See the `AltMediatR.Samples` project for:

- DI setup, handler registration, and behavior configuration.
- Example command/query/void handlers and pre/post processors.
- Domain/integration events with a console publisher and in-memory outbox.
