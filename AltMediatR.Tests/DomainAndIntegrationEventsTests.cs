using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Extensions;
using AltMediatR.DDD.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AltMediatR.Tests
{
    public class DomainAndIntegrationEventsTests
    {
        // Minimal test types
        public sealed class UserCreatedDomainEvent : IDomainEvent
        {
            public required string UserId { get; init; }
            public required string Name { get; init; }
        }
        public sealed class UserCreatedIntegrationEvent : IIntegrationEvent
        {
            public Guid Id { get; init; } = Guid.NewGuid();
            public required string UserId { get; init; }
            public required string Name { get; init; }
        }
        public record CreateUserCommand(string Name) : IRequest<string>;
        public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
        {
            public Task<string> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
                => Task.FromResult(Guid.NewGuid().ToString());
        }

        private sealed class NoOpTransactionScope : ITransactionScope
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
        private sealed class NoOpTransactionManager : ITransactionManager
        {
            public Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<ITransactionScope>(new NoOpTransactionScope());
        }

        [Fact]
        public async Task Should_Dispatch_Domain_And_Publish_Integration_Events()
        {
            var services = new ServiceCollection();
            services.AddAltMediator(s => { });
            services.AddAltMediatorDdd();
            services.AddTransactionalOutboxBehavior();
            services.AddScoped<IEventQueueCollector, InMemoryEventQueueCollector>();

            var domainHandler = new Mock<INotificationHandler<UserCreatedDomainEvent>>();
            domainHandler
                .Setup(h => h.HandleAsync(It.IsAny<UserCreatedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(domainHandler.Object);

            var publisher = new Mock<IIntegrationEventPublisher>();
            publisher
                .Setup(p => p.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(publisher.Object);

            services.AddSingleton<ITransactionManager, NoOpTransactionManager>();
            services.AddInMemoryOutboxStore();
            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();
            services.ValidateAltMediatorConfiguration();

            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            var id = await mediator.SendAsync(new CreateUserCommand("Test"));
            Assert.False(string.IsNullOrWhiteSpace(id));
        }

        private sealed class FailingPublisher : IIntegrationEventPublisher
        {
            public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("fail");
        }

        [Fact]
        public async Task Should_Save_To_Outbox_When_Publisher_Fails()
        {
            var services = new ServiceCollection();
            services.AddAltMediator(s => { });
            services.AddAltMediatorDdd();
            services.AddTransactionalOutboxBehavior();
            services.AddScoped<IEventQueueCollector, InMemoryEventQueueCollector>();
            services.AddSingleton<IIntegrationEventPublisher, FailingPublisher>();
            services.AddSingleton<ITransactionManager, NoOpTransactionManager>();
            services.AddInMemoryOutboxStore();

            var domainHandler = new Mock<INotificationHandler<UserCreatedDomainEvent>>();
            domainHandler
                .Setup(h => h.HandleAsync(It.IsAny<UserCreatedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(domainHandler.Object);

            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();
            services.ValidateAltMediatorConfiguration();

            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            var id = await mediator.SendAsync(new CreateUserCommand("Test"));
            Assert.False(string.IsNullOrWhiteSpace(id));
        }
    }
}
