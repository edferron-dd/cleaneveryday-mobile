namespace CleanEverydayMobile.Views;

public partial class TemperatureCheckerPage : ContentPage
{
    private readonly ILogger<TemperatureCheckerPage> _logger;

    public TemperatureCheckerPage(ILogger<TemperatureCheckerPage> logger)
    {
        InitializeComponent();
        _logger = logger;
    }

    private void OnCheckClicked(object sender, EventArgs e)
    {
        var input = TemperatureEntry.Text?.Trim() ?? "0";
        int.TryParse(input, out int temperature);

        _logger.LogInformation("Checking temperature: {Temperature}", temperature);

        int result = temperature / 0;
    }
}
