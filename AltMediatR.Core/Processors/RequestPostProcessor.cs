using AltMediatR.Core.Abstractions;

namespace AltMediatR.Core.Processors
{
    public class RequestPostProcessor<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IRequestPostProcessor<TRequest, TResponse>> _postProcessors;
        private readonly IRequestHandler<TRequest, TResponse> _handler;

        public RequestPostProcessor(
            IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors,
            IRequestHandler<TRequest, TResponse> handler)
        {
            _postProcessors = postProcessors;
            _handler = handler;
        }

        public async Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken)
        {
            // HandleAsync the request
            var response = await _handler.HandleAsync(request, cancellationToken);

            // Run post-processors
            foreach (var postProcessor in _postProcessors)
            {
                await postProcessor.ProcessAsync(request, response, cancellationToken);
            }

            return response;
        }
    }
}
