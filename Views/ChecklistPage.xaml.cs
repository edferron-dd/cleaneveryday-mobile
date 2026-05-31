using CleanEverydayMobile.Models;
using CleanEverydayMobile.Services;

namespace CleanEverydayMobile.Views;

public partial class ChecklistPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly ILogger<ChecklistPage> _logger;
    private List<ChecklistItem> _items = new();
    private string? _checklistId;

    public ChecklistPage(ApiService api, SessionService session, ILogger<ChecklistPage> logger)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _logger = logger;
        _logger.LogInformation("ChecklistPage loaded");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("ChecklistPage appearing");
        await LoadChecklistAsync();
    }

    private async Task LoadChecklistAsync()
    {
        _logger.LogInformation("Loading checklist for userId: {UserId}", _session.UserId);
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var checklists = await _api.GetChecklistAsync(_session.UserId!);

            if (checklists.Count == 0)
            {
                // Create a default checklist for new users
                var response = await _api.CreateChecklistAsync(_session.UserId!, "My Checklist");
                _checklistId = response?.Id;
                _items = new List<ChecklistItem>();
            }
            else
            {
                var first = checklists[0];
                _checklistId = first.Id;
                _items = first.Items;
            }

            ChecklistItemsView.ItemsSource = _items;
            _logger.LogInformation("Checklist loaded with {Count} items, checklistId: {ChecklistId}", _items.Count, _checklistId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklist");
            ErrorLabel.Text = "Failed to load checklist.";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        var text = NewItemEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text) || _checklistId == null) return;

        _logger.LogInformation("Adding item to checklist {ChecklistId}: {Text}", _checklistId, text);

        try
        {
            var newItem = await _api.AddChecklistItemAsync(_checklistId, text);
            if (newItem != null)
            {
                _items.Add(newItem);
                ChecklistItemsView.ItemsSource = null;
                ChecklistItemsView.ItemsSource = _items;
                NewItemEntry.Text = string.Empty;
                _logger.LogInformation("Item added: {ItemId}", newItem.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to checklist");
            await DisplayAlert("Error", "Failed to add item.", "OK");
        }
    }

    private async void OnItemCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (_checklistId == null) return;

        var itemId = checkBox.AutomationId;
        _logger.LogInformation("Toggling item {ItemId} in checklist {ChecklistId}", itemId, _checklistId);

        try
        {
            await _api.ToggleChecklistItemAsync(_checklistId, itemId);
            _logger.LogInformation("Item {ItemId} toggled", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling item {ItemId}", itemId);
        }
    }
}
