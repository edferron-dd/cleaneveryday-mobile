using System.Net.Http.Json;
using System.Text.Json;
using CleanEverydayMobile.Models;

namespace CleanEverydayMobile.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiService> _logger;
    private const string BaseUrl = "https://dd-cleaneveryday-api.azurewebsites.net";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, ILogger<ApiService> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for {Username}", username);
        try
        {
            var response = await _http.PostAsJsonAsync("/api/v1/login", new { username, password });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed for {Username}: {StatusCode}", username, response.StatusCode);
                return null;
            }
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            _logger.LogInformation("Login successful for {Username}", username);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Username}", username);
            throw;
        }
    }

    public async Task<UserProfile?> GetProfileAsync(string userId)
    {
        _logger.LogInformation("GetProfile for userId: {UserId}", userId);
        try
        {
            return await _http.GetFromJsonAsync<UserProfile>($"/api/v1/profile/{userId}", JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProfile error for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<LocationItem>> GetLocationsAsync()
    {
        _logger.LogInformation("GetLocations called");
        try
        {
            return await _http.GetFromJsonAsync<List<LocationItem>>("/api/v1/locations", JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLocations error");
            throw;
        }
    }

    public async Task<UserLocationResponse?> GetUserLocationAsync(string userId)
    {
        _logger.LogInformation("GetUserLocation for userId: {UserId}", userId);
        try
        {
            return await _http.GetFromJsonAsync<UserLocationResponse>($"/api/v1/locations/{userId}", JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserLocation error for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task SaveUserLocationAsync(string userId, string location)
    {
        _logger.LogInformation("SaveUserLocation for userId: {UserId}, location: {Location}", userId, location);
        try
        {
            await _http.PostAsJsonAsync($"/api/v1/locations/{userId}", new { location });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveUserLocation error for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Checklist>> GetChecklistAsync(string userId)
    {
        _logger.LogInformation("GetChecklist for userId: {UserId}", userId);
        try
        {
            return await _http.GetFromJsonAsync<List<Checklist>>($"/api/v1/checklist/{userId}", JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetChecklist error for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Checklist?> CreateChecklistAsync(string userId, string name)
    {
        _logger.LogInformation("CreateChecklist for userId: {UserId}, name: {Name}", userId, name);
        try
        {
            var response = await _http.PostAsJsonAsync("/api/v1/checklist/add", new { userId, name });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Checklist>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateChecklist error for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<ChecklistItem?> AddChecklistItemAsync(string checklistId, string text)
    {
        _logger.LogInformation("AddChecklistItem to checklistId: {ChecklistId}", checklistId);
        try
        {
            var response = await _http.PostAsJsonAsync($"/api/v1/checklist/{checklistId}/item", new { text });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChecklistItem>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddChecklistItem error for checklistId: {ChecklistId}", checklistId);
            throw;
        }
    }

    public async Task<ChecklistItem?> ToggleChecklistItemAsync(string checklistId, string itemId)
    {
        _logger.LogInformation("ToggleChecklistItem checklistId: {ChecklistId}, itemId: {ItemId}", checklistId, itemId);
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/v1/checklist/{checklistId}/item/{itemId}", new { });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChecklistItem>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ToggleChecklistItem error for itemId: {ItemId}", itemId);
            throw;
        }
    }

    public async Task<List<Printer>> GetPrintersAsync()
    {
        _logger.LogInformation("GetPrinters called");
        try
        {
            return await _http.GetFromJsonAsync<List<Printer>>("/api/v1/printer/list", JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPrinters error");
            throw;
        }
    }

    public async Task SelectPrinterAsync(string printerId)
    {
        _logger.LogInformation("SelectPrinter: {PrinterId}", printerId);
        try
        {
            await _http.PutAsJsonAsync($"/api/v1/printer/{printerId}", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SelectPrinter error for printerId: {PrinterId}", printerId);
            throw;
        }
    }
}
