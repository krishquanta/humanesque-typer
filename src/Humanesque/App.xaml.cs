using System.IO;
using System.Windows;
using Humanesque.Services;
using Humanesque.Views;

namespace Humanesque;

public partial class App : Application
{
    private readonly ConfigurationService _configService = new();

    public App()
    {
        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Humanesque");
        Directory.CreateDirectory(logDir);

        DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                File.WriteAllText(Path.Combine(logDir, "crash.log"), e.Exception.ToString());
            }
            catch { }
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                File.WriteAllText(Path.Combine(logDir, "crash_domain.log"), e.ExceptionObject.ToString());
            }
            catch { }
        };
    }

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
    }
}
