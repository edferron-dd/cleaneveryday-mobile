using CleanEverydayMobile.Services;

namespace CleanEverydayMobile.Views;

public partial class HomePage : ContentPage
{
    private readonly SessionService _session;
    private readonly ILogger<HomePage> _logger;

    public HomePage(SessionService session, ILogger<HomePage> logger)
    {
        InitializeComponent();
        _session = session;
        _logger = logger;
        _logger.LogInformation("HomePage loaded");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("HomePage appearing for userId: {UserId}", _session.UserId);
        FullnameBanner.Text = _session.Fullname ?? "Welcome";
        LocationBanner.Text = _session.Location ?? "No location set";
    }

    private async void OnChecklistClicked(object sender, EventArgs e)
    {
        _logger.LogInformation("Navigating to ChecklistPage");
        await Shell.Current.GoToAsync("ChecklistPage");
    }

    private async void OnPrintersClicked(object sender, EventArgs e)
    {
        _logger.LogInformation("Navigating to PrintersPage");
        await Shell.Current.GoToAsync("PrintersPage");
    }

    private async void OnTemperatureCheckerClicked(object sender, EventArgs e)
    {
        _logger.LogInformation("Navigating to TemperatureCheckerPage");
        await Shell.Current.GoToAsync("TemperatureCheckerPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _logger.LogInformation("Logout tapped for userId: {UserId}", _session.UserId);
        _session.Clear();
        await Shell.Current.GoToAsync("//LoginPage");
        _logger.LogInformation("Logged out, navigated to LoginPage");
    }
}
