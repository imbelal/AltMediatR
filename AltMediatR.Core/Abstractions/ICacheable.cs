namespace AltMediatR.Core.Abstractions
{
    // Requests that want to be cached should implement this to provide a stable key and optional TTL.
    public interface ICacheable
    {
        string CacheKey { get; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    }
}
