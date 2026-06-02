using CleanEverydayMobile.Models;
using CleanEverydayMobile.Services;

namespace CleanEverydayMobile.Views;

public partial class PrintersPage : ContentPage
{
    private readonly ApiService _api;
    private readonly ILogger<PrintersPage> _logger;
    private Printer? _selectedPrinter;

    public PrintersPage(ApiService api, ILogger<PrintersPage> logger)
    {
        InitializeComponent();
        _api = api;
        _logger = logger;
        _logger.LogInformation("PrintersPage loaded");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPrintersAsync();
    }

    private async Task LoadPrintersAsync()
    {
        _logger.LogInformation("Loading printers");
        try
        {
            var printers = await _api.GetPrintersAsync();
            PrintersList.ItemsSource = printers;
            _logger.LogInformation("Loaded {Count} printers", printers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading printers");
            ErrorLabel.Text = "Failed to load printers.";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void OnPrinterSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedPrinter = e.CurrentSelection.FirstOrDefault() as Printer;
        OkButton.IsEnabled = _selectedPrinter != null;
        _logger.LogInformation("Printer selected: {PrinterName}", _selectedPrinter?.Name);
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        if (_selectedPrinter == null) return;

        _logger.LogInformation("OK clicked, selecting printer: {PrinterId}", _selectedPrinter.Id);
        try
        {
            await _api.SelectPrinterAsync(_selectedPrinter.Id);
            _logger.LogInformation("Printer selected successfully, navigating back");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting printer: {PrinterId}", _selectedPrinter.Id);
            await DisplayAlert("Error", "Failed to select printer.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _logger.LogInformation("Cancel clicked on PrintersPage, navigating back");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnNewPrinterClicked(object sender, EventArgs e)
    {
        await _api.CreatePrinterAsync();
    }
}
