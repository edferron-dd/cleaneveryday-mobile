using CleanEverydayMobile.Views;

namespace CleanEverydayMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("ChecklistPage", typeof(ChecklistPage));
        Routing.RegisterRoute("PrintersPage", typeof(PrintersPage));
        Routing.RegisterRoute("TemperatureCheckerPage", typeof(TemperatureCheckerPage));
    }
}
