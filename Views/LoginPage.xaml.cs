using CleanEverydayMobile.Services;

namespace CleanEverydayMobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly ILogger<LoginPage> _logger;

    public LoginPage(ApiService api, SessionService session, ILogger<LoginPage> logger)
    {
        InitializeComponent();
        _api = api;
        _session = session;
        _logger = logger;
        _logger.LogInformation("LoginPage loaded");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Please enter username and password.";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        _logger.LogInformation("Login button tapped for user: {Username}", username);

        try
        {
            var result = await _api.LoginAsync(username, password);
            if (result == null)
            {
                _logger.LogWarning("Login returned null for {Username}", username);
                ErrorLabel.Text = "Invalid username or password.";
                ErrorLabel.IsVisible = true;
                return;
            }

            _session.SetSession(result.UserId, result.Fullname, result.Location);
            _logger.LogInformation("Login success, navigating to home. userId: {UserId}", result.UserId);

            if (string.IsNullOrEmpty(result.Location))
            {
                _logger.LogInformation("No location for user {UserId}, navigating to location selection", result.UserId);
                await Shell.Current.GoToAsync("//LocationSelectionPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//HomePage");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login exception for {Username}", username);
            ErrorLabel.Text = "Connection error. Please try again.";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}
