using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Extensions;
using AltMediatR.Samples.Commands;
using AltMediatR.Samples.Events;
using AltMediatR.Samples.Processors;
using AltMediatR.Samples.Queries;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using AltMediatR.Samples.Infrastructure;
using AltMediatR.DDD.Infrastructure;


var services = new ServiceCollection();

// Add logging
services.AddLogging();
services.AddMemoryCache();

// Register the mediator + handlers
services.AddAltMediator(s =>
{
    s.AddLoggingBehavior()
    .AddValidationBehavior()
    .AddPerformanceBehavior()
    .AddRetryBehavior();
});
services.AddAltMediatorDdd();
services.AddTransactionalOutboxBehavior();
services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Infrastructure for DDD demo
services.AddSingleton<IIntegrationEventPublisher, ConsoleIntegrationEventPublisher>();
services.AddSingleton<ITransactionManager, NoOpTransactionManager>();
services.AddInMemoryOutboxStore();
services.AddScoped<IEventQueueCollector, InMemoryEventQueueCollector>();

// Validate AltMediator configuration (fail fast if duplicates or behavior issues)
services.ValidateAltMediatorConfiguration(validateBehaviors: true);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

// Sample command: Create user (will queue domain + integration events)
var userId = await mediator.SendAsync(new CreateUserCommand { Name = "John Doe" });
Console.WriteLine($"User created with ID: {userId}");

// Sample query: Get user
var userInfo = await mediator.SendAsync(new GetUserQuery { UserId = userId });
Console.WriteLine(userInfo);

// Sample void command: Delete user
await mediator.SendAsync(new DeleteUserCommand { UserId = userId });
Console.WriteLine("Delete completed");

// Additionally publish simple sample events as notifications
var sampleEvent = new SampleEvent()
{
    Message = $"Hello from {nameof(SampleEvent)}"
};
await mediator.PublishAsync(sampleEvent);

var anotherEvent = new AnotherEvent()
{
    Message = $"Hello from {nameof(AnotherEvent)}"
};
await mediator.PublishAsync(anotherEvent);
