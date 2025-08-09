using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using AltMediatR.Samples.Events;
using AltMediatR.Samples.Processors;
using AltMediatR.Samples.Queries;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


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
    .AddRetryBehavior()
    .AddCachingBehavior();
});
services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());
services.RegisterRequestPreProcessor(typeof(LoggingPreProcessor<>));
services.RegisterRequestPostProcessor(typeof(LoggingPostProcessor<,>));

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

// Sample command: Create user
var userId = await mediator.SendAsync(new CreateUserCommand { Name = "John Doe" });
Console.WriteLine($"User created with ID: {userId}");

// Sample query: Get user
var userInfo = await mediator.SendAsync(new GetUserQuery { UserId = userId });
Console.WriteLine(userInfo);

// Sample events
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
