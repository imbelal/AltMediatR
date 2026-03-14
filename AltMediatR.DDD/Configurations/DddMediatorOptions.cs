namespace AltMediatR.DDD.Configurations
{
    /// <summary>
    /// Controls the order in which domain and integration events are dispatched.
    /// </summary>
    public enum DispatchOrder
    {
        /// <summary>Domain events are dispatched before integration events.</summary>
        DomainFirst,
        /// <summary>Integration events are dispatched before domain events.</summary>
        IntegrationFirst
    }

    /// <summary>
    /// Options for configuring the DDD mediator's transactional event dispatch behavior.
    /// </summary>
    public sealed class DddMediatorOptions
    {
        /// <summary>
        /// The order in which domain and integration events are dispatched.
        /// Defaults to <see cref="DispatchOrder.DomainFirst"/>.
        /// </summary>
        public DispatchOrder DispatchOrder { get; set; } = DispatchOrder.DomainFirst;

        /// <summary>
        /// When true, domain and integration events are dispatched in parallel.
        /// Defaults to false (sequential dispatch).
        /// </summary>
        public bool ParallelDispatch { get; set; } = false;
    }
}
