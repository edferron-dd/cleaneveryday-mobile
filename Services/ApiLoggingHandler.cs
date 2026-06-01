namespace CleanEverydayMobile.Services;

public class ApiLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ApiLoggingHandler> _logger;

    public ApiLoggingHandler(ILogger<ApiLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("API Request: {Method} {Uri}", request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            _logger.LogInformation("  REQ {Name}: {Value}", header.Key, string.Join(", ", header.Value));

        if (request.Content?.Headers != null)
            foreach (var header in request.Content.Headers)
                _logger.LogInformation("  REQ {Name}: {Value}", header.Key, string.Join(", ", header.Value));

        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogInformation("API Response: {StatusCode} {Uri}", (int)response.StatusCode, request.RequestUri);

        return response;
    }
}
