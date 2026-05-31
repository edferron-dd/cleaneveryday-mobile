namespace CleanEverydayMobile;

public partial class App : Application
{
    public App(AppShell shell)
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
