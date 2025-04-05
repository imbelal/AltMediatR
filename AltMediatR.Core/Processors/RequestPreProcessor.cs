using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Processors
{
    public class RequestPreProcessor<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IRequestPreProcessor<TRequest>> _postProcessors;
        private readonly IRequestHandler<TRequest, TResponse> _handler;

        public RequestPreProcessor(
            IEnumerable<IRequestPreProcessor<TRequest>> postProcessors,
            IRequestHandler<TRequest, TResponse> handler)
        {
            _postProcessors = postProcessors;
            _handler = handler;
        }

        public async Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken)
        {
            // Run pre-processors
            foreach (var postProcessor in _postProcessors)
            {
                await postProcessor.ProcessAsync(request, cancellationToken);
            }

            // HandleAsync the request
            var response = await _handler.HandleAsync(request, cancellationToken);

            return response;
        }
    }
}
