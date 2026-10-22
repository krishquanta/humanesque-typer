using System.Windows;
using Humanesque.Services;
using Humanesque.Views;

namespace Humanesque;

public partial class App : Application
{
    private readonly ConfigurationService _configService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = _configService.LoadSettings();

        // If first run, present the onboarding setup wizard
        if (settings.IsFirstRun)
        {
            var onboarding = new OnboardingWindow();
            bool? result = onboarding.ShowDialog();

            settings.IsFirstRun = false;
            _configService.SaveSettings(settings);
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
