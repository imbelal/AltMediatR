using System.Reflection;
using AltMediatR.DDD.Abstractions;
using AltMediatR.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace AltMediatR.WebApiSample.Infrastructure;

public sealed class EfChangeTrackerEventCollector : IEventQueueCollector
{
    private readonly DbContext _dbContext;

    public EfChangeTrackerEventCollector(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<IDomainEvent> CollectDomainEvents()
    {
        return GetAggregates()
            .SelectMany(GetDomainEvents)
            .ToArray();
    }

    public IEnumerable<IIntegrationEvent> CollectIntegrationEvents()
    {
        return GetAggregates()
            .SelectMany(GetIntegrationEvents)
            .ToArray();
    }

    public void ClearEvents()
    {
        foreach (var agg in GetAggregates())
        {
            InvokeClearEvents(agg);
        }
    }

    private IEnumerable<AggregateRootBase> GetAggregates()
    {
        return _dbContext.ChangeTracker
            .Entries()
            .Where(e => e.Entity is AggregateRootBase)
            .Select(e => (AggregateRootBase)e.Entity)
            .ToArray();
    }

    private static IReadOnlyCollection<IDomainEvent> GetDomainEvents(AggregateRootBase aggregate)
    {
        var prop = typeof(AggregateRootBase).GetProperty("DomainEvents", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IReadOnlyCollection<IDomainEvent>?)prop?.GetValue(aggregate) ?? Array.Empty<IDomainEvent>();
    }

    private static IReadOnlyCollection<IIntegrationEvent> GetIntegrationEvents(AggregateRootBase aggregate)
    {
        var prop = typeof(AggregateRootBase).GetProperty("IntegrationEvents", BindingFlags.Instance | BindingFlags.NonPublic);
        return (IReadOnlyCollection<IIntegrationEvent>?)prop?.GetValue(aggregate) ?? Array.Empty<IIntegrationEvent>();
    }

    private static void InvokeClearEvents(AggregateRootBase aggregate)
    {
        var method = typeof(AggregateRootBase).GetMethod("ClearEvents", BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(aggregate, null);
    }
}
