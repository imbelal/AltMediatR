using AltMediatR.Core.Extensions;
using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Extensions;
using AltMediatR.WebApiSample.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AltMediatR.WebApiSample;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddMemoryCache();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o => o.CustomSchemaIds(t => t.FullName));

        // EF Core InMemory
        builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("app-db"));

        // AltMediatR Core + DDD
        builder.Services
            // Mediator setup
            .AddAltMediator(s => { })
            .AddHandlersFromAssembly(Assembly.GetExecutingAssembly())

            // DDD integration
            .AddDddIntegrationDdd()
            .AddDddHandlersFromAssembly(Assembly.GetExecutingAssembly())

            // Query caching
            .AddCachingForQueries(_ => { /* can override defaults here */ })

            // Outbox pattern
            .AddInMemoryOutboxStore()
            .AddInMemoryOutboxProcessor()
            .AddOutboxProcessorHostedService(TimeSpan.FromSeconds(5))
            .AddTransactionalOutboxBehavior()

            // Messaging
            .AddInMemoryInboundMessageProcessor()
            .UseInMemoryLoopbackPublisher();

        // Use EF ChangeTracker-based event collector and transaction manager
        builder.Services.AddScoped<IEventQueueCollector, EfChangeTrackerEventCollector>();
        builder.Services.AddScoped<ITransactionManager, EfTransactionManager>();

        var app = builder.Build();

        // Start inbound processor
        var inbound = app.Services.GetRequiredService<AltMediatR.DDD.Infrastructure.InMemoryInboundMessageProcessor>();
        _ = inbound.StartAsync(CancellationToken.None);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
