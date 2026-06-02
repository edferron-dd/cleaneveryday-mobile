using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;

namespace CleanEverydayMobile.Services;

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
    private const string OperationName = "http.client.request";

    private static readonly Lazy<DatadogTraceMethods?> TraceMethods = new(CreateTraceMethods);

    private readonly HashSet<string> _firstPartyHosts;

    public DatadogTracingHandler(IEnumerable<string> firstPartyHosts)
    {
        _firstPartyHosts = firstPartyHosts
                           .Where(host => !string.IsNullOrWhiteSpace(host))
                           .Select(NormalizeHost)
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!ShouldInjectHeaders(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        TraceHeaderContext? traceContext = null;
        try
        {
            traceContext = TraceHeaderContext.Create();
            InjectTraceHeaders(request, traceContext);
        }
        catch
        {
            traceContext = null;
        }

        var datadogSpanId = TryStartDatadogSpan(request);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            TryFinishDatadogSpan(datadogSpanId, response, null);
            return response;
        }
        catch (Exception ex)
        {
            TryFinishDatadogSpan(datadogSpanId, null, ex);
            throw;
        }
        finally
        {
            traceContext.Activity?.Stop();
            traceContext.Activity?.Dispose();
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

    private static string? TryStartDatadogSpan(HttpRequestMessage request)
    {
        var traceMethods = TraceMethods.Value;
        if (traceMethods is null)
        {
            return null;
        }

        try
        {
            var context = new Dictionary<string, object>
            {
                ["span.kind"] = "client",
                                ["http.method"] = request.Method.Method,
                                                  ["http.url"] = request.RequestUri?.ToString() ?? string.Empty
            };

            var spanId = traceMethods.StartSpan.Invoke(
                             null,
                             new object?[]
            {
                OperationName,
                context,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            return spanId as string ?? spanId?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void TryFinishDatadogSpan(string? spanId, HttpResponseMessage? response, Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(spanId))
        {
            return;
        }

        var traceMethods = TraceMethods.Value;
        if (traceMethods is null)
        {
            return;
        }

        try
        {
            var context = new Dictionary<string, object>();
            if (response is not null)
            {
                context["http.status_code"] = (int)response.StatusCode;
            }

            if (exception is not null)
            {
                context["error.message"] = exception.Message;
                context["error.type"] = exception.GetType().FullName ?? exception.GetType().Name;
            }

            traceMethods.FinishSpan.Invoke(
                null,
                new object?[]
            {
                spanId,
                context,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
        catch
        {
        }
    }

    private static DatadogTraceMethods? CreateTraceMethods()
    {
        var ddTraceType = Type.GetType("DatadogSdk.Maui.DdTrace, DatadogSdk.Maui");
        if (ddTraceType is null)
        {
            return null;
        }

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;
        var signature = new[] { typeof(string), typeof(Dictionary<string, object>), typeof(long) };
        var startSpan = ddTraceType.GetMethod(
                            "StartSpan",
                            bindingFlags,
                            binder: null,
                            types: signature,
                            modifiers: null);
        var finishSpan = ddTraceType.GetMethod(
                             "FinishSpan",
                             bindingFlags,
                             binder: null,
                             types: signature,
                             modifiers: null);

        return startSpan is null || finishSpan is null
               ? null
               : new DatadogTraceMethods(startSpan, finishSpan);
    }

    private static ulong ParseHexAsUInt64(string value) =>
    ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
    ? parsed
    : 0;

    private static ulong GenerateRandomUInt64NonZero()
    {
        Span<byte> bytes = stackalloc byte[8];
        ulong value = 0;
        while (value == 0)
        {
            RandomNumberGenerator.Fill(bytes);
            value = BitConverter.ToUInt64(bytes);
        }

        return value;
    }

    private sealed record DatadogTraceMethods(MethodInfo StartSpan, MethodInfo FinishSpan);

    private sealed record TraceHeaderContext(
        ulong DatadogTraceId,
        ulong DatadogSpanId,
        int SamplingPriority,
        string TraceParent,
        string TraceState,
        string? DatadogTags,
        Activity? Activity)
    {
        public static TraceHeaderContext Create()
        {
            var activity = new Activity(OperationName);
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            var traceIdHex = activity.TraceId.ToHexString().ToLowerInvariant();
            var spanIdHex = activity.SpanId.ToHexString().ToLowerInvariant();
            var datadogTraceId = ParseHexAsUInt64(traceIdHex[^16..]);
            var datadogSpanId = ParseHexAsUInt64(spanIdHex);

            if (datadogTraceId != 0 && datadogSpanId != 0)
            {
                return Build(activity, traceIdHex, spanIdHex, datadogTraceId, datadogSpanId, 1);
            }

            activity.Stop();
            activity.Dispose();

            // DdTrace.StartSpan does not expose trace/span identifiers, so fallback propagation IDs are generated locally.
            var fallbackTraceId = GenerateRandomUInt64NonZero();
            var fallbackSpanId = GenerateRandomUInt64NonZero();
            var fallbackTraceIdHighBits = GenerateRandomUInt64NonZero();
            var fallbackTraceIdHex = FormattableString.Invariant($"{fallbackTraceIdHighBits:x16}{fallbackTraceId:x16}");
            var fallbackSpanIdHex = FormattableString.Invariant($"{fallbackSpanId:x16}");

            return Build(
                       activity: null,
                       traceIdHex: fallbackTraceIdHex,
                       spanIdHex: fallbackSpanIdHex,
                       datadogTraceId: fallbackTraceId,
                       datadogSpanId: fallbackSpanId,
                       samplingPriority: 1);
        }

        private static TraceHeaderContext Build(
            Activity? activity,
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
                       Activity: activity);
        }
    }
}