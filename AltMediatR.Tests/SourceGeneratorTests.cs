using AltMediatR.Core.Abstractions;
using AltMediatR.Core.Extensions;
using AltMediatR.Generated;
using Microsoft.Extensions.DependencyInjection;

namespace AltMediatR.Tests
{
    // ---------------------------------------------------------------------------
    // Concrete handler implementations at namespace scope so that the source
    // generator can discover them and include them in AddGeneratedHandlers().
    // ---------------------------------------------------------------------------

    public record DoubleValueQuery(int Value) : IRequest<int>;

    public sealed class DoubleValueHandler : IRequestHandler<DoubleValueQuery, int>
    {
        public Task<int> HandleAsync(DoubleValueQuery request, CancellationToken cancellationToken)
            => Task.FromResult(request.Value * 2);
    }

    public record EchoNotification(string Message) : INotification;

    public sealed class EchoNotificationHandler : INotificationHandler<EchoNotification>
    {
        public static string? LastMessage { get; set; }

        public Task HandleAsync(EchoNotification @event, CancellationToken cancellationToken)
        {
            LastMessage = @event.Message;
            return Task.CompletedTask;
        }
    }

    // ---------------------------------------------------------------------------
    // Tests verifying that AddGeneratedHandlers() wires up the above handlers
    // without using runtime reflection.
    // ---------------------------------------------------------------------------

    public class SourceGeneratorTests
    {
        [Fact]
        public async Task AddGeneratedHandlers_Registers_RequestHandler_And_Mediator_Dispatches_Correctly()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers(); // compile-time generated registration

            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();

            var result = await mediator.SendAsync(new DoubleValueQuery(21));

            Assert.Equal(42, result);
        }

        [Fact]
        public async Task AddGeneratedHandlers_Registers_NotificationHandler_And_Mediator_Publishes_Correctly()
        {
            EchoNotificationHandler.LastMessage = null;

            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers();

            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();

            await mediator.PublishAsync(new EchoNotification("hello from generator"));

            Assert.Equal("hello from generator", EchoNotificationHandler.LastMessage);
        }

        [Fact]
        public void AddGeneratedHandlers_Registers_Handler_In_ServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddGeneratedHandlers();

            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IRequestHandler<DoubleValueQuery, int>));

            Assert.NotNull(descriptor);
            Assert.Equal(typeof(DoubleValueHandler), descriptor!.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }
    }
}
