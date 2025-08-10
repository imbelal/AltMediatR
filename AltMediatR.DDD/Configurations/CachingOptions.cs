namespace AltMediatR.DDD.Configurations
{
    public class CachingOptions
    {
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);
        public string? KeyPrefix { get; set; }
    }
}
