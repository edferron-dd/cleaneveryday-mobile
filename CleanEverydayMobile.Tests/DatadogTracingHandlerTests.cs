using System.Globalization;
using System.Net;
using CleanEverydayMobile.Services;
using CleanEverydayMobile.Tests.Helpers;

namespace CleanEverydayMobile.Tests;

public class DatadogTracingHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsTracingHeaders_AndRumTraceAttributes_ForFirstPartyHost()
    {
        var rumTracker = new FakeRumResourceTracker();
        var (client, innerHandler) = BuildClient(rumTracker, "dd-cleaneveryday-api.azurewebsites.net");

        await client.GetAsync("https://dd-cleaneveryday-api.azurewebsites.net/api/v1/locations");

        Assert.Single(innerHandler.Requests);
        var request = innerHandler.Requests[0];

        var datadogTraceId = AssertNumericHeader(request, "x-datadog-trace-id");
        var datadogParentId = AssertNumericHeader(request, "x-datadog-parent-id");
        Assert.Equal("1", request.Headers.GetValues("x-datadog-sampling-priority").Single());
        Assert.Equal("rum", request.Headers.GetValues("x-datadog-origin").Single());

        var traceParent = request.Headers.GetValues("traceparent").Single();
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-01$", traceParent);
        var traceParentParts = traceParent.Split('-');
        Assert.Equal(4, traceParentParts.Length);
        var traceIdFromTraceParent = ParseHexAsUInt64(traceParentParts[1][^16..]);
        var spanIdFromTraceParent = ParseHexAsUInt64(traceParentParts[2]);
        Assert.Equal(datadogTraceId, traceIdFromTraceParent);
        Assert.Equal(datadogParentId, spanIdFromTraceParent);

        var traceState = request.Headers.GetValues("tracestate").Single();
        Assert.Contains("dd=s:1", traceState);

        Assert.Single(rumTracker.StartCalls);
        var startCall = rumTracker.StartCalls[0];
        Assert.Equal(datadogParentId.ToString(CultureInfo.InvariantCulture), startCall.ResourceKey);
        Assert.Equal("https://dd-cleaneveryday-api.azurewebsites.net/api/v1/locations", startCall.Url);
        Assert.Equal("GET", startCall.Method);
        Assert.Equal(datadogTraceId.ToString(CultureInfo.InvariantCulture), Assert.IsType<string>(startCall.TraceAttributes["_dd.trace_id"]));
        Assert.Equal(datadogParentId.ToString(CultureInfo.InvariantCulture), Assert.IsType<string>(startCall.TraceAttributes["_dd.span_id"]));
        Assert.Equal(1.0d, Assert.IsType<double>(startCall.TraceAttributes["_dd.rule_psr"]));

        Assert.Single(rumTracker.StopCalls);
        var stopCall = rumTracker.StopCalls[0];
        Assert.Equal(startCall.ResourceKey, stopCall.ResourceKey);
        Assert.Equal((int)HttpStatusCode.OK, stopCall.StatusCode);
        Assert.Null(stopCall.Exception);
        Assert.Equal(datadogTraceId.ToString(CultureInfo.InvariantCulture), Assert.IsType<string>(stopCall.TraceAttributes["_dd.trace_id"]));
        Assert.Equal(datadogParentId.ToString(CultureInfo.InvariantCulture), Assert.IsType<string>(stopCall.TraceAttributes["_dd.span_id"]));
        Assert.Equal(1.0d, Assert.IsType<double>(stopCall.TraceAttributes["_dd.rule_psr"]));
    }

    [Fact]
    public async Task SendAsync_SkipsTracingHeaders_ForNonFirstPartyHost()
    {
        var rumTracker = new FakeRumResourceTracker();
        var (client, innerHandler) = BuildClient(rumTracker, "dd-cleaneveryday-api.azurewebsites.net");

        await client.GetAsync("https://example.com/api/v1/locations");

        Assert.Single(innerHandler.Requests);
        var request = innerHandler.Requests[0];

        Assert.False(request.Headers.Contains("x-datadog-trace-id"));
        Assert.False(request.Headers.Contains("x-datadog-parent-id"));
        Assert.False(request.Headers.Contains("x-datadog-sampling-priority"));
        Assert.False(request.Headers.Contains("x-datadog-origin"));
        Assert.False(request.Headers.Contains("x-datadog-tags"));
        Assert.False(request.Headers.Contains("traceparent"));
        Assert.False(request.Headers.Contains("tracestate"));
        Assert.Empty(rumTracker.StartCalls);
        Assert.Empty(rumTracker.StopCalls);
    }

    private static (HttpClient client, MockHttpMessageHandler handler) BuildClient(
        IRumResourceTracker? rumResourceTracker,
        params string[] firstPartyHosts)
    {
        var innerHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var tracingHandler = rumResourceTracker is null
                             ? new DatadogTracingHandler(firstPartyHosts)
                             : new DatadogTracingHandler(firstPartyHosts, rumResourceTracker);
        tracingHandler.InnerHandler = innerHandler;

        var client = new HttpClient(tracingHandler);
        return (client, innerHandler);
    }

    private static ulong AssertNumericHeader(HttpRequestMessage request, string headerName)
    {
        var value = request.Headers.GetValues(headerName).Single();
        Assert.True(ulong.TryParse(value, out var parsed));
        Assert.NotEqual(0UL, parsed);
        return parsed;
    }

    private static ulong ParseHexAsUInt64(string value)
    {
        Assert.True(ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed));
        Assert.NotEqual(0UL, parsed);
        return parsed;
    }

    private sealed class FakeRumResourceTracker : IRumResourceTracker
    {
        public List<RumStartCall> StartCalls
        {
            get;
        } = [];
        public List<RumStopCall> StopCalls
        {
            get;
        } = [];

        public void StartResource(
            string resourceKey,
            HttpRequestMessage request,
            Dictionary<string, object> traceAttributes,
            long timestampMs)
        {
            StartCalls.Add(
                new RumStartCall(
                    ResourceKey: resourceKey,
                    Method: request.Method.Method,
                    Url: request.RequestUri?.ToString(),
                    TraceAttributes: new Dictionary<string, object>(traceAttributes)));
        }

        public void StopResource(
            string resourceKey,
            HttpResponseMessage? response,
            Exception? exception,
            Dictionary<string, object> traceAttributes,
            long timestampMs)
        {
            StopCalls.Add(
                new RumStopCall(
                    ResourceKey: resourceKey,
                    StatusCode: response is null ? 0 : (int)response.StatusCode,
                    Exception: exception,
                    TraceAttributes: new Dictionary<string, object>(traceAttributes)));
        }
    }

    private sealed record RumStartCall(
        string ResourceKey,
        string Method,
        string? Url,
        Dictionary<string, object> TraceAttributes);

    private sealed record RumStopCall(
        string ResourceKey,
        int StatusCode,
        Exception? Exception,
        Dictionary<string, object> TraceAttributes);
}
