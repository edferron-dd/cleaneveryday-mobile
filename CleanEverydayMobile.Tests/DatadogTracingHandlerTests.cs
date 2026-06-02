using System.Net;
using CleanEverydayMobile.Services;
using CleanEverydayMobile.Tests.Helpers;

namespace CleanEverydayMobile.Tests;

public class DatadogTracingHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsTracingHeaders_ForFirstPartyHost()
    {
        var (client, innerHandler) = BuildClient("dd-cleaneveryday-api.azurewebsites.net");

        await client.GetAsync("https://dd-cleaneveryday-api.azurewebsites.net/api/v1/locations");

        Assert.Single(innerHandler.Requests);
        var request = innerHandler.Requests[0];

        AssertNumericHeader(request, "x-datadog-trace-id");
        AssertNumericHeader(request, "x-datadog-parent-id");
        Assert.Equal("1", request.Headers.GetValues("x-datadog-sampling-priority").Single());
        Assert.Equal("rum", request.Headers.GetValues("x-datadog-origin").Single());

        var traceParent = request.Headers.GetValues("traceparent").Single();
        Assert.Matches("^00-[0-9a-f]{32}-[0-9a-f]{16}-01$", traceParent);

        var traceState = request.Headers.GetValues("tracestate").Single();
        Assert.Contains("dd=s:1", traceState);
    }

    [Fact]
    public async Task SendAsync_SkipsTracingHeaders_ForNonFirstPartyHost()
    {
        var (client, innerHandler) = BuildClient("dd-cleaneveryday-api.azurewebsites.net");

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
    }

    private static (HttpClient client, MockHttpMessageHandler handler) BuildClient(params string[] firstPartyHosts)
    {
        var innerHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var tracingHandler = new DatadogTracingHandler(firstPartyHosts)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(tracingHandler);
        return (client, innerHandler);
    }

    private static void AssertNumericHeader(HttpRequestMessage request, string headerName)
    {
        var value = request.Headers.GetValues(headerName).Single();
        Assert.True(ulong.TryParse(value, out var parsed));
        Assert.NotEqual(0UL, parsed);
    }
}