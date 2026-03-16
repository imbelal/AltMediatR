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

    // Void command — discovered by the generator and dispatched via TryDispatchVoid
    public sealed class IncrementCommand : IRequest { }

    public sealed class IncrementHandler : IRequestHandler<IncrementCommand>
    {
        public static int Count { get; set; }

        public Task HandleAsync(IncrementCommand request, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    // ---------------------------------------------------------------------------
    // Tests verifying that AddGeneratedHandlers() wires up handlers and the
    // compiled dispatcher without using runtime reflection.
    // ---------------------------------------------------------------------------

    public class SourceGeneratorTests
    {
        [Fact]
        public async Task AddGeneratedHandlers_Registers_RequestHandler_And_Mediator_Dispatches_Correctly()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers(); // compile-time generated registration + dispatcher

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
        public async Task AddGeneratedHandlers_Registers_VoidHandler_And_CompiledDispatcher_Executes_Void_Correctly()
        {
            IncrementHandler.Count = 0;

            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers();

            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();

            await mediator.SendAsync(new IncrementCommand());

            Assert.Equal(1, IncrementHandler.Count);
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

        [Fact]
        public void AddGeneratedHandlers_Registers_CompiledDispatcher_As_Singleton()
        {
            var services = new ServiceCollection();
            services.AddGeneratedHandlers();

            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(ICompiledHandlerDispatcher));

            Assert.NotNull(descriptor);
            Assert.Equal(typeof(GeneratedHandlerDispatcher), descriptor!.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void CompiledDispatcher_TryDispatch_Returns_True_For_Known_Request_Type()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers();

            var sp = services.BuildServiceProvider();
            var dispatcher = sp.GetRequiredService<ICompiledHandlerDispatcher>();
            var pipelineDispatcher = (IPipelineDispatcher)sp.GetRequiredService<IMediator>();

            var handled = dispatcher.TryDispatch(
                pipelineDispatcher,
                new DoubleValueQuery(5),
                CancellationToken.None,
                out var task);

            Assert.True(handled);
            Assert.NotNull(task);
        }

        [Fact]
        public void CompiledDispatcher_TryDispatchVoid_Returns_True_For_Known_Void_Request_Type()
        {
            var services = new ServiceCollection();
            services.AddAltMediator();
            services.AddGeneratedHandlers();

            var sp = services.BuildServiceProvider();
            var dispatcher = sp.GetRequiredService<ICompiledHandlerDispatcher>();
            var pipelineDispatcher = (IPipelineDispatcher)sp.GetRequiredService<IMediator>();

            var handled = dispatcher.TryDispatchVoid(
                pipelineDispatcher,
                new IncrementCommand(),
                CancellationToken.None,
                out var task);

            Assert.True(handled);
            Assert.NotNull(task);
        }
    }
}
