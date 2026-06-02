using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace CleanEverydayMobile.Services;

internal interface IRumResourceTracker
{
    void StartResource(
        string resourceKey,
        HttpRequestMessage request,
        Dictionary<string, object> traceAttributes,
        long timestampMs);

    void StopResource(
        string resourceKey,
        HttpResponseMessage? response,
        Exception? exception,
        Dictionary<string, object> traceAttributes,
        long timestampMs);
}

public sealed class DatadogTracingHandler : DelegatingHandler
{
    private const string DatadogTraceIdHeader = "x-datadog-trace-id";
    private const string DatadogParentIdHeader = "x-datadog-parent-id";
    private const string DatadogSamplingPriorityHeader = "x-datadog-sampling-priority";
    private const string DatadogOriginHeader = "x-datadog-origin";
    private const string DatadogTagsHeader = "x-datadog-tags";
    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";
    private const string DatadogOriginValue = "rum";
    private const string RumTraceIdAttribute = "_dd.trace_id";
    private const string RumSpanIdAttribute = "_dd.span_id";
    private const string RumRulePsrAttribute = "_dd.rule_psr";
    private const string RumTraceIdHighBitsAttribute = "_dd.p.tid";
    private const int DefaultSamplingPriority = 1;
    private const string ResourceKindXhr = "Xhr";

    private static readonly Lazy<RumMethods?> RumMethodsCache = new(CreateRumMethods);
    private static readonly Dictionary<string, string> HttpMethodToRumMethodName = new(StringComparer.OrdinalIgnoreCase)
    {
        { HttpMethod.Get.Method, "Get" },
        { HttpMethod.Post.Method, "Post" },
        { HttpMethod.Put.Method, "Put" },
        { HttpMethod.Delete.Method, "Delete" },
        { HttpMethod.Head.Method, "Head" },
        { HttpMethod.Patch.Method, "Patch" },
        { HttpMethod.Options.Method, "Options" },
        { HttpMethod.Trace.Method, "Trace" },
        { HttpMethod.Connect.Method, "Connect" }
    };

    private readonly HashSet<string> _firstPartyHosts;
    private readonly IRumResourceTracker _rumResourceTracker;

    public DatadogTracingHandler(IEnumerable<string> firstPartyHosts)
        : this(firstPartyHosts, ReflectionRumResourceTracker.Instance)
    {
    }

    internal DatadogTracingHandler(IEnumerable<string> firstPartyHosts, IRumResourceTracker rumResourceTracker)
    {
        _firstPartyHosts = firstPartyHosts
                           .Where(host => !string.IsNullOrWhiteSpace(host))
                           .Select(NormalizeHost)
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _rumResourceTracker = rumResourceTracker;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!ShouldInjectHeaders(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var traceContext = TraceHeaderContext.Create();
        InjectTraceHeaders(request, traceContext);
        var rumResourceContext = TryStartRumResource(request, traceContext);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            TryStopRumResource(rumResourceContext, response, null);
            return response;
        }
        catch (Exception ex)
        {
            TryStopRumResource(rumResourceContext, null, ex);
            throw;
        }
    }

    private bool ShouldInjectHeaders(Uri? requestUri)
    {
        if (requestUri?.Host is null || _firstPartyHosts.Count == 0)
        {
            return false;
        }

        var requestHost = NormalizeHost(requestUri.Host);
        return _firstPartyHosts.Any(firstPartyHost =>
                                    string.Equals(requestHost, firstPartyHost, StringComparison.OrdinalIgnoreCase)
                                    || requestHost.EndsWith($".{firstPartyHost}", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeHost(string host) => host.Trim().TrimEnd('.').ToLowerInvariant();

    private static void InjectTraceHeaders(HttpRequestMessage request, TraceHeaderContext context)
    {
        AddHeaderIfMissing(request, DatadogTraceIdHeader, context.DatadogTraceId.ToString(CultureInfo.InvariantCulture));
        AddHeaderIfMissing(request, DatadogParentIdHeader, context.DatadogSpanId.ToString(CultureInfo.InvariantCulture));
        AddHeaderIfMissing(request, DatadogSamplingPriorityHeader, context.SamplingPriority.ToString(CultureInfo.InvariantCulture));
        AddHeaderIfMissing(request, DatadogOriginHeader, DatadogOriginValue);
        AddHeaderIfMissing(request, TraceParentHeader, context.TraceParent);
        AddHeaderIfMissing(request, TraceStateHeader, context.TraceState);

        if (!string.IsNullOrWhiteSpace(context.DatadogTags))
        {
            AddHeaderIfMissing(request, DatadogTagsHeader, context.DatadogTags);
        }
    }

    private static void AddHeaderIfMissing(HttpRequestMessage request, string headerName, string value)
    {
        if (request.Headers.Contains(headerName) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(headerName, value);
    }

    private RumResourceContext? TryStartRumResource(HttpRequestMessage request, TraceHeaderContext traceContext)
    {
        var resourceContext = new RumResourceContext(
            ResourceKey: traceContext.DatadogSpanId.ToString(CultureInfo.InvariantCulture),
            TraceAttributes: traceContext.CreateRumTraceAttributes());

        try
        {
            _rumResourceTracker.StartResource(
                resourceContext.ResourceKey,
                request,
                resourceContext.TraceAttributes,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return resourceContext;
        }
        catch
        {
            return null;
        }
    }

    private void TryStopRumResource(RumResourceContext? resourceContext, HttpResponseMessage? response, Exception? exception)
    {
        if (resourceContext is null)
        {
            return;
        }

        try
        {
            _rumResourceTracker.StopResource(
                resourceContext.ResourceKey,
                response,
                exception,
                resourceContext.TraceAttributes,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        catch
        {
        }
    }

    private static RumMethods? CreateRumMethods()
    {
        var ddRumType = Type.GetType("DatadogSdk.Maui.DdRum, DatadogSdk.Maui");
        if (ddRumType is null)
        {
            return null;
        }

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;
        var startResource = ddRumType.GetMethods(bindingFlags)
                            .FirstOrDefault(method =>
                                            method.Name == "StartResource"
                                            && method.GetParameters().Length == 5);
        var stopResource = ddRumType.GetMethods(bindingFlags)
                           .FirstOrDefault(method =>
                                           method.Name == "StopResource"
                                           && method.GetParameters().Length == 6);
        if (startResource is null || stopResource is null)
        {
            return null;
        }

        var startParameters = startResource.GetParameters();
        var stopParameters = stopResource.GetParameters();
        var resourceMethodType = startParameters[1].ParameterType;
        var resourceKindType = stopParameters[2].ParameterType;
        if (!resourceMethodType.IsEnum || !resourceKindType.IsEnum)
        {
            return null;
        }

        var resourceKind = ParseEnumValue(resourceKindType, ResourceKindXhr);
        if (resourceKind is null)
        {
            return null;
        }

        var resourceMethods = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (httpMethod, rumMethodName) in HttpMethodToRumMethodName)
        {
            var resourceMethod = ParseEnumValue(resourceMethodType, rumMethodName);
            if (resourceMethod is not null)
            {
                resourceMethods[httpMethod] = resourceMethod;
            }
        }

        return resourceMethods.Count == 0
               ? null
               : new RumMethods(startResource, stopResource, resourceKind, resourceMethods);
    }

    private static ulong ParseHexAsUInt64(string value) =>
    ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
    ? parsed
    : 0;

    private static object? ParseEnumValue(Type enumType, string value)
    {
        try
        {
            return Enum.Parse(enumType, value, ignoreCase: true);
        }
        catch
        {
            return null;
        }
    }

    private sealed class ReflectionRumResourceTracker : IRumResourceTracker
    {
        public static ReflectionRumResourceTracker Instance
        {
            get;
        } = new();

        public void StartResource(
            string resourceKey,
            HttpRequestMessage request,
            Dictionary<string, object> traceAttributes,
            long timestampMs)
        {
            var rumMethods = RumMethodsCache.Value;
            if (rumMethods is null || request.RequestUri is null)
            {
                return;
            }

            var resourceMethod = rumMethods.ResolveResourceMethod(request.Method.Method);
            if (resourceMethod is null)
            {
                return;
            }

            rumMethods.StartResource.Invoke(
                null,
                new object?[]
            {
                resourceKey,
                resourceMethod,
                request.RequestUri.ToString(),
                new Dictionary<string, object>(traceAttributes),
                timestampMs
            });
        }

        public void StopResource(
            string resourceKey,
            HttpResponseMessage? response,
            Exception? exception,
            Dictionary<string, object> traceAttributes,
            long timestampMs)
        {
            var rumMethods = RumMethodsCache.Value;
            if (rumMethods is null)
            {
                return;
            }

            var context = new Dictionary<string, object>(traceAttributes);
            if (response is not null)
            {
                context["http.status_code"] = (int)response.StatusCode;
            }

            if (exception is not null)
            {
                context["error.message"] = exception.Message;
                context["error.type"] = exception.GetType().FullName ?? exception.GetType().Name;
            }

            var statusCode = response is null ? 0 : (int)response.StatusCode;
            var responseSize = response?.Content?.Headers.ContentLength ?? -1L;

            rumMethods.StopResource.Invoke(
                null,
                new object?[]
            {
                resourceKey,
                statusCode,
                rumMethods.ResourceKind,
                responseSize,
                context,
                timestampMs
            });
        }
    }

    private sealed record RumMethods(
        MethodInfo StartResource,
        MethodInfo StopResource,
        object ResourceKind,
        IReadOnlyDictionary<string, object> ResourceMethods)
    {
        public object? ResolveResourceMethod(string httpMethod) =>
        ResourceMethods.TryGetValue(httpMethod, out var resourceMethod)
        ? resourceMethod
        : null;
    }

    private sealed record RumResourceContext(
        string ResourceKey,
        Dictionary<string, object> TraceAttributes);

    private sealed record TraceHeaderContext(
        ulong DatadogTraceId,
        ulong DatadogSpanId,
        int SamplingPriority,
        string TraceParent,
        string TraceState,
        string? DatadogTags,
        string? DatadogTraceIdHighBitsHex)
    {
        public static TraceHeaderContext Create()
        {
            while (true)
            {
                var traceIdHex = ActivityTraceId.CreateRandom().ToHexString().ToLowerInvariant();
                var spanIdHex = ActivitySpanId.CreateRandom().ToHexString().ToLowerInvariant();
                var datadogTraceId = ParseHexAsUInt64(traceIdHex[^16..]);
                var datadogSpanId = ParseHexAsUInt64(spanIdHex);
                if (datadogTraceId == 0 || datadogSpanId == 0)
                {
                    continue;
                }

                return Build(traceIdHex, spanIdHex, datadogTraceId, datadogSpanId, DefaultSamplingPriority);
            }
        }

        public Dictionary<string, object> CreateRumTraceAttributes()
        {
            var attributes = new Dictionary<string, object>();
            attributes[RumTraceIdAttribute] = DatadogTraceId.ToString(CultureInfo.InvariantCulture);
            attributes[RumSpanIdAttribute] = DatadogSpanId.ToString(CultureInfo.InvariantCulture);
            attributes[RumRulePsrAttribute] = 1.0d;

            if (!string.IsNullOrWhiteSpace(DatadogTraceIdHighBitsHex))
            {
                attributes[RumTraceIdHighBitsAttribute] = DatadogTraceIdHighBitsHex;
            }

            return attributes;
        }

        private static TraceHeaderContext Build(
            string traceIdHex,
            string spanIdHex,
            ulong datadogTraceId,
            ulong datadogSpanId,
            int samplingPriority)
        {
            var highBits = traceIdHex[..16];
            var traceParent = FormattableString.Invariant($"00-{traceIdHex}-{spanIdHex}-{(samplingPriority > 0 ? "01" : "00")}");
            var traceState = highBits == "0000000000000000"
                             ? FormattableString.Invariant($"dd=s:{samplingPriority};p:{spanIdHex}")
                             : FormattableString.Invariant($"dd=s:{samplingPriority};p:{spanIdHex};t.tid:{highBits}");
            var datadogTags = highBits == "0000000000000000"
                              ? null
                              : FormattableString.Invariant($"_dd.p.tid={highBits}");

            return new TraceHeaderContext(
                       DatadogTraceId: datadogTraceId,
                       DatadogSpanId: datadogSpanId,
                       SamplingPriority: samplingPriority,
                       TraceParent: traceParent,
                       TraceState: traceState,
                       DatadogTags: datadogTags,
                       DatadogTraceIdHighBitsHex: highBits == "0000000000000000" ? null : highBits);
        }
    }
}
