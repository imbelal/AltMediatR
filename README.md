# AltMediatR

**AltMediatR** is a lightweight alternative to the MediatR library for implementing the Mediator pattern in .NET applications. It provides a simple, in-memory mediator for handling commands, queries, and notifications with support for dependency injection and pipeline behaviors.

## Table of Contents
- [Overview](#overview)
- [Installation](#installation)
- [Key Components](#key-components)
- [Usage](#usage)
  - [Handling Commands](#handling-commands)
  - [Handling Queries](#handling-queries)
  - [Publishing Notifications](#publishing-notifications)
  - [Using Pipeline Behaviors](#using-pipeline-behaviors)
- [Dependency Injection](#dependency-injection)
- [Contributing](#contributing)
- [License](#license)

## Overview
AltMediatR enables decoupled communication between components in a .NET application using the Mediator pattern. Key features include:
- **Commands**: For state-changing operations (e.g., creating or updating data).
- **Queries**: For retrieving data.
- **Notifications**: For broadcasting events to multiple handlers.
- **Pipeline Behaviors**: For cross-cutting concerns like logging or validation.
- **Dependency Injection**: Seamless integration with `Microsoft.Extensions.DependencyInjection`.

The library is designed to be lightweight, flexible, and easy to integrate into existing .NET projects.

## Installation
To use AltMediatR in your project:
1. Clone the repository:
   ```bash
   git clone https://github.com/imbelal/AltMediatR.git
   ```
2. Add the project to your solution or include the source files in your .NET project.
3. Ensure you have the required dependency: `Microsoft.Extensions.DependencyInjection`.


## Key Components
- **`IRequest<TResponse>`**: Interface for requests (commands or queries) that return a response of type `TResponse`.
- **`IRequestHandler<TRequest, TResponse>`**: Interface for handling specific requests.
- **`IMediator`**: Core interface for sending requests and publishing notifications.
  - `Send`: Sends a command or query and returns a response.
  - `Publish`: Broadcasts a notification to all registered handlers.
- **`INotification` and `INotificationHandler<TNotification>`**: Interfaces for defining and handling notifications.
- **`IPipelineBehavior<TRequest, TResponse>`**: Interface for pipeline behaviors to handle cross-cutting concerns.

## Usage

### Handling Commands
Define a command and its handler:
```csharp
public class CreateUserCommand : ICommand<int>
{
    public string Name { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, int>
{
    public Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Logic to create a user
        return Task.FromResult(42); // Example: return user ID
    }
}
```

Send the command:
```csharp
var mediator = serviceProvider.GetService<IMediator>();
var command = new CreateUserCommand { Name = "John Doe" };
int userId = await mediator.Send(command);
Console.WriteLine($"User created with ID: {userId}");
```

### Handling Queries
Define a query and its handler:
```csharp
public class GetUserQuery : IQuery<User>
{
    public int Id { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    public Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Logic to retrieve a user
        return Task.FromResult(new User { Id = request.Id, Name = "John Doe" });
    }
}
```

Send the query:
```csharp
var mediator = serviceProvider.GetService<IMediator>();
var query = new GetUserQuery { Id = 42 };
var user = await mediator.Send(query);
Console.WriteLine($"User: {user.Name}");
```

### Publishing Notifications
Define a notification and its handler:
```csharp
public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
}

public class UserCreatedNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Notification: User {notification.UserId} created.");
        return Task.CompletedTask;
    }
}
```

Publish the notification:
```csharp
var mediator = serviceProvider.GetService<IMediator>();
var notification = new UserCreatedNotification { UserId = 42 };
await mediator.Publish(notification);
```

### Using Pipeline Behaviors
Define a pipeline behavior (e.g., for logging):
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

Register the behavior:
```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

The behavior will wrap request handling automatically.

## Dependency Injection
AltMediatR integrates with `Microsoft.Extensions.DependencyInjection`. Register services in your `Startup.cs` or equivalent:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMediatR(typeof(Program).Assembly); // Registers handlers and mediator
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)); // Optional: Add behaviors
}
```

The `AddMediatR` extension method scans the assembly for handlers and registers them.

## Contributing
Contributions are welcome! To contribute:
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a pull request.

Please ensure your code adheres to the project's coding standards and includes tests where applicable.

## License
This project does not currently specify a license. Contact the repository owner (imbelal) for licensing details.
