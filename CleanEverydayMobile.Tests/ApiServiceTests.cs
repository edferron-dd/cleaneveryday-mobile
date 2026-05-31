using System.Net;
using System.Text;
using System.Text.Json;
using CleanEverydayMobile.Models;
using CleanEverydayMobile.Services;
using CleanEverydayMobile.Tests.Helpers;
using Moq;

namespace CleanEverydayMobile.Tests;

public class ApiServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static (ApiService service, MockHttpMessageHandler handler) Build(
        HttpStatusCode status, object? body)
    {
        var json = body is not null ? JsonSerializer.Serialize(body) : "null";
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var handler = new MockHttpMessageHandler(response);
        var http = new HttpClient(handler);
        var logger = new Mock<ILogger<ApiService>>().Object;
        return (new ApiService(http, logger), handler);
    }

    // LoginAsync

    [Fact]
    public async Task LoginAsync_ReturnsLoginResponse_WhenSuccessful()
    {
        var expected = new LoginResponse { UserId = "u1", Fullname = "Alice", Username = "alice" };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.LoginAsync("alice", "secret");

        Assert.NotNull(result);
        Assert.Equal("u1", result.UserId);
        Assert.Equal("Alice", result.Fullname);
        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenStatusIsNotSuccess()
    {
        var (svc, _) = Build(HttpStatusCode.Unauthorized, null);

        var result = await svc.LoginAsync("alice", "wrong");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_OnForbidden()
    {
        var (svc, _) = Build(HttpStatusCode.Forbidden, null);

        var result = await svc.LoginAsync("alice", "wrong");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_SendsPostToLoginEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new LoginResponse { UserId = "u1" });

        await svc.LoginAsync("alice", "secret");

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("/api/v1/login", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task LoginAsync_ThrowsHttpRequestException_WhenNetworkFails()
    {
        var handler = new MockHttpMessageHandler(_ => throw new HttpRequestException("network error"));
        var http = new HttpClient(handler);
        var svc = new ApiService(http, new Mock<ILogger<ApiService>>().Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.LoginAsync("alice", "secret"));
    }

    // GetProfileAsync

    [Fact]
    public async Task GetProfileAsync_ReturnsUserProfile_WhenSuccessful()
    {
        var expected = new UserProfile { Id = "u1", Fullname = "Alice", Username = "alice" };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.GetProfileAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result.Id);
        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task GetProfileAsync_SendsGetToProfileEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new UserProfile { Id = "u1" });

        await svc.GetProfileAsync("u1");

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("/api/v1/profile/u1", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // GetLocationsAsync

    [Fact]
    public async Task GetLocationsAsync_ReturnsLocationList_WhenSuccessful()
    {
        var expected = new List<LocationItem>
        {
            new() { Id = "loc1", Name = "Plant A" },
            new() { Id = "loc2", Name = "Plant B" }
        };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.GetLocationsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Plant A", result[0].Name);
        Assert.Equal("Plant B", result[1].Name);
    }

    [Fact]
    public async Task GetLocationsAsync_ReturnsEmptyList_WhenResponseIsNull()
    {
        var (svc, _) = Build(HttpStatusCode.OK, null);

        var result = await svc.GetLocationsAsync();

        Assert.Empty(result);
    }

    // GetUserLocationAsync

    [Fact]
    public async Task GetUserLocationAsync_ReturnsUserLocationResponse_WhenSuccessful()
    {
        var expected = new UserLocationResponse { UserId = "u1", Location = "Plant A" };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.GetUserLocationAsync("u1");

        Assert.NotNull(result);
        Assert.Equal("u1", result.UserId);
        Assert.Equal("Plant A", result.Location);
    }

    [Fact]
    public async Task GetUserLocationAsync_SendsGetToUserLocationEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new UserLocationResponse { UserId = "u1" });

        await svc.GetUserLocationAsync("u1");

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Contains("/api/v1/locations/u1", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // SaveUserLocationAsync

    [Fact]
    public async Task SaveUserLocationAsync_SendsPostRequest()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new { });

        await svc.SaveUserLocationAsync("u1", "Plant A");

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
    }

    [Fact]
    public async Task SaveUserLocationAsync_SendsToUserLocationEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new { });

        await svc.SaveUserLocationAsync("u1", "Plant A");

        Assert.Contains("/api/v1/locations/u1", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // GetChecklistAsync

    [Fact]
    public async Task GetChecklistAsync_ReturnsChecklistList_WhenSuccessful()
    {
        var expected = new List<Checklist>
        {
            new() { Id = "c1", Name = "Morning Checklist", UserId = "u1" }
        };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.GetChecklistAsync("u1");

        Assert.Single(result);
        Assert.Equal("Morning Checklist", result[0].Name);
        Assert.Equal("u1", result[0].UserId);
    }

    [Fact]
    public async Task GetChecklistAsync_ReturnsEmptyList_WhenResponseIsNull()
    {
        var (svc, _) = Build(HttpStatusCode.OK, null);

        var result = await svc.GetChecklistAsync("u1");

        Assert.Empty(result);
    }

    // CreateChecklistAsync

    [Fact]
    public async Task CreateChecklistAsync_ReturnsChecklist_WhenSuccessful()
    {
        var expected = new Checklist { Id = "c1", Name = "Morning", UserId = "u1" };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.CreateChecklistAsync("u1", "Morning");

        Assert.NotNull(result);
        Assert.Equal("c1", result.Id);
        Assert.Equal("Morning", result.Name);
    }

    [Fact]
    public async Task CreateChecklistAsync_ThrowsHttpRequestException_WhenStatusIsNotSuccess()
    {
        var (svc, _) = Build(HttpStatusCode.BadRequest, null);

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.CreateChecklistAsync("u1", "Morning"));
    }

    [Fact]
    public async Task CreateChecklistAsync_SendsPostToChecklistEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new Checklist { Id = "c1" });

        await svc.CreateChecklistAsync("u1", "Morning");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("/api/v1/checklist/add", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // AddChecklistItemAsync

    [Fact]
    public async Task AddChecklistItemAsync_ReturnsChecklistItem_WhenSuccessful()
    {
        var expected = new ChecklistItem { Id = "i1", ChecklistId = "c1", Text = "Mop floor", Status = false };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.AddChecklistItemAsync("c1", "Mop floor");

        Assert.NotNull(result);
        Assert.Equal("i1", result.Id);
        Assert.Equal("Mop floor", result.Text);
        Assert.False(result.Status);
    }

    [Fact]
    public async Task AddChecklistItemAsync_ThrowsHttpRequestException_WhenStatusIsNotSuccess()
    {
        var (svc, _) = Build(HttpStatusCode.InternalServerError, null);

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.AddChecklistItemAsync("c1", "Mop floor"));
    }

    [Fact]
    public async Task AddChecklistItemAsync_SendsPostToChecklistItemEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new ChecklistItem { Id = "i1" });

        await svc.AddChecklistItemAsync("c1", "Mop floor");

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Contains("/api/v1/checklist/c1/item", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // ToggleChecklistItemAsync

    [Fact]
    public async Task ToggleChecklistItemAsync_ReturnsUpdatedItem_WhenSuccessful()
    {
        var expected = new ChecklistItem { Id = "i1", ChecklistId = "c1", Text = "Mop floor", Status = true };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.ToggleChecklistItemAsync("c1", "i1");

        Assert.NotNull(result);
        Assert.Equal("i1", result.Id);
        Assert.True(result.Status);
    }

    [Fact]
    public async Task ToggleChecklistItemAsync_ThrowsHttpRequestException_WhenStatusIsNotSuccess()
    {
        var (svc, _) = Build(HttpStatusCode.NotFound, null);

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.ToggleChecklistItemAsync("c1", "i1"));
    }

    [Fact]
    public async Task ToggleChecklistItemAsync_SendsPutToChecklistItemEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new ChecklistItem { Id = "i1" });

        await svc.ToggleChecklistItemAsync("c1", "i1");

        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
        Assert.Contains("/api/v1/checklist/c1/item/i1", handler.Requests[0].RequestUri!.PathAndQuery);
    }

    // GetPrintersAsync

    [Fact]
    public async Task GetPrintersAsync_ReturnsPrinterList_WhenSuccessful()
    {
        var expected = new List<Printer>
        {
            new() { Id = "p1", Name = "Printer A", Status = "online" },
            new() { Id = "p2", Name = "Printer B", Status = "offline" }
        };
        var (svc, _) = Build(HttpStatusCode.OK, expected);

        var result = await svc.GetPrintersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Printer A", result[0].Name);
        Assert.Equal("online", result[0].Status);
    }

    [Fact]
    public async Task GetPrintersAsync_ReturnsEmptyList_WhenResponseIsNull()
    {
        var (svc, _) = Build(HttpStatusCode.OK, null);

        var result = await svc.GetPrintersAsync();

        Assert.Empty(result);
    }

    // SelectPrinterAsync

    [Fact]
    public async Task SelectPrinterAsync_SendsPutRequest()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new { });

        await svc.SelectPrinterAsync("p1");

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
    }

    [Fact]
    public async Task SelectPrinterAsync_SendsToPrinterEndpoint()
    {
        var (svc, handler) = Build(HttpStatusCode.OK, new { });

        await svc.SelectPrinterAsync("p1");

        Assert.Contains("/api/v1/printer/p1", handler.Requests[0].RequestUri!.PathAndQuery);
    }
}
