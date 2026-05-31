using CleanEverydayMobile.Models;
using CleanEverydayMobile.Services;

namespace CleanEverydayMobile.Views;

public partial class LocationSelectionPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly ILogger<LocationSelectionPage> _logger;

    public LocationSelectionPage(ApiService api, SessionService session, ILogger<LocationSelectionPage> logger)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _logger = logger;
        _logger.LogInformation("LocationSelectionPage loaded");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLocationsAsync();
    }

    private async Task LoadLocationsAsync()
    {
        _logger.LogInformation("Loading locations");
        try
        {
            var locations = await _api.GetLocationsAsync();
            LocationsList.ItemsSource = locations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading locations");
            ErrorLabel.Text = "Failed to load locations.";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnLocationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not LocationItem selected) return;

        _logger.LogInformation("Location selected: {Location} for userId: {UserId}", selected.Name, _session.UserId);

        try
        {
            await _api.SaveUserLocationAsync(_session.UserId!, selected.Name);
            _session.SetLocation(selected.Name);
            _logger.LogInformation("Location saved, navigating to HomePage");
            await Shell.Current.GoToAsync("//HomePage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving location");
            ErrorLabel.Text = "Failed to save location.";
            ErrorLabel.IsVisible = true;
        }
    }
}
