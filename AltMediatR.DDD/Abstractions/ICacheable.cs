namespace AltMediatR.DDD.Abstractions
{
    public interface ICacheable
    {
        string CacheKey { get; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    }
}
