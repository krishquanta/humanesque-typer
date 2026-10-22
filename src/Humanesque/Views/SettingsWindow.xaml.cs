using System.Windows;
using Humanesque.Services;

namespace Humanesque.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigurationService _configService = new();

    public SettingsWindow()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var settings = _configService.LoadSettings();
        ChkFloatingPill.IsChecked = settings.ShowFloatingPill;
        ChkSound.IsChecked = settings.PlayCompletionSound;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var settings = _configService.LoadSettings();
        settings.ShowFloatingPill = ChkFloatingPill.IsChecked == true;
        settings.PlayCompletionSound = ChkSound.IsChecked == true;

        _configService.SaveSettings(settings);
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
