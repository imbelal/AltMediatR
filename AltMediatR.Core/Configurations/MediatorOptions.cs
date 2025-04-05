namespace AltMediatR.Core.Configurations
{
    public class MediatorOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public bool EnableCaching { get; set; } = true;
    }
}
