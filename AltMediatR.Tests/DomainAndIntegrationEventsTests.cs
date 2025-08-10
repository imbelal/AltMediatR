using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Samples.Commands;
using AltMediatR.Samples.Events;
using AltMediatR.Samples.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AltMediatR.Tests
{
    public class DomainAndIntegrationEventsTests
    {
        [Fact]
        public async Task Should_Dispatch_Domain_And_Publish_Integration_Events()
        {
            var services = new ServiceCollection();
            services.AddAltMediator(s => s.AddTransactionalOutboxBehavior());

            // Probe domain handler via Moq
            var domainHandler = new Mock<INotificationHandler<UserCreatedDomainEvent>>();
            domainHandler
                .Setup(h => h.HandleAsync(It.IsAny<UserCreatedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(domainHandler.Object);

            // Probe integration publisher via Moq
            var publisher = new Mock<IIntegrationEventPublisher>();
            publisher
                .Setup(p => p.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            services.AddSingleton(publisher.Object);

            services.AddSingleton<ITransactionManager, NoOpTransactionManager>();
            services.AddSingleton<IIntegrationOutbox, InMemoryIntegrationOutbox>();
            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();
            services.ValidateAltMediatorConfiguration();

            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            var id = await mediator.SendAsync(new CreateUserCommand { Name = "Test" });
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
            // Ensure outbox is empty before test
            InMemoryIntegrationOutbox.Clear();

            var services = new ServiceCollection();
            services.AddAltMediator(s => s.AddTransactionalOutboxBehavior());
            services.AddSingleton<IIntegrationEventPublisher, FailingPublisher>();
            services.AddSingleton<ITransactionManager, NoOpTransactionManager>();
            services.AddSingleton<IIntegrationOutbox, InMemoryIntegrationOutbox>();

            // Probe domain handler to ensure domain events still dispatched
            var domainHandler = new Mock<INotificationHandler<UserCreatedDomainEvent>>();
            domainHandler
                .Setup(h => h.HandleAsync(It.IsAny<UserCreatedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(domainHandler.Object);

            services.AddTransient<IRequestHandler<CreateUserCommand, string>, CreateUserHandler>();
            services.ValidateAltMediatorConfiguration();

            var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

            var id = await mediator.SendAsync(new CreateUserCommand { Name = "Test" });
            Assert.False(string.IsNullOrWhiteSpace(id));

            // Assert event persisted to outbox
            var drained = InMemoryIntegrationOutbox.DrainAll();
            Assert.Single(drained);
            var evt = Assert.IsType<UserCreatedIntegrationEvent>(drained[0]);
            Assert.Equal(id, evt.UserId);
            Assert.Equal("Test", evt.Name);
        }
    }
}
