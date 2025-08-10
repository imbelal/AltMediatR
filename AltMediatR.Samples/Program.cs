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
    .AddCachingForQueries(o => { o.DefaultTtl = TimeSpan.FromMinutes(2); o.KeyPrefix = "sample:"; });
});
services.RegisterHandlersFromAssembly(Assembly.GetExecutingAssembly());
services.RegisterRequestPreProcessor(typeof(LoggingPreProcessor<>));
services.RegisterRequestPostProcessor(typeof(LoggingPostProcessor<,>));

// Validate AltMediator configuration (fail fast if duplicates or behavior issues)
services.ValidateAltMediatorConfiguration(validateBehaviors: true);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

// Sample command: Create user
var userId = await mediator.SendAsync(new CreateUserCommand { Name = "John Doe" });
Console.WriteLine($"User created with ID: {userId}");

// Sample query: Get user
var userInfo = await mediator.SendAsync(new GetUserQuery { UserId = userId });
Console.WriteLine(userInfo);

// Sample void command: Delete user
await mediator.SendAsync(new DeleteUserCommand { UserId = userId });
Console.WriteLine("Delete completed");

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
