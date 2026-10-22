using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Humanesque.Models;
using Humanesque.Services;

namespace Humanesque.Views;

public partial class OnboardingWindow : Window
{
    private readonly CalibrationService _calibrationService = new();
    private readonly ConfigurationService _configService = new();
    private Stopwatch? _testStopwatch;
    private CalibrationResult? _calibrationResult;

    public OnboardingWindow()
    {
        InitializeComponent();
    }

    private void OnTestInputChanged(object sender, TextChangedEventArgs e)
    {
        string typed = TxtTestInput.Text;
        if (typed.Length == 1 && _testStopwatch == null)
        {
            _testStopwatch = Stopwatch.StartNew();
            TxtCalibrationStatus.Text = "Status: Test active... keep typing!";
        }
        else if (_testStopwatch != null && typed.Length > 10)
        {
            _calibrationResult = _calibrationService.EvaluateTypingTest(
                CalibrationService.SampleText,
                typed,
                _testStopwatch.Elapsed
            );

            TxtCalibrationStatus.Text = $"Status: Calibrated at {_calibrationResult.MeasuredWpm:F0} WPM ({_calibrationResult.AccuracyPercent:F0}% accuracy)";
        }
    }

    private void OnFinishClicked(object sender, RoutedEventArgs e)
    {
        var settings = _configService.LoadSettings();

        if (_calibrationResult != null && _calibrationResult.MeasuredWpm > 15)
        {
            settings.CustomProfiles.Add(_calibrationResult.CalibratedProfile);
            settings.ActiveProfileId = _calibrationResult.CalibratedProfile.Id;
        }
        else if (RadioSlow.IsChecked == true)
        {
            settings.ActiveProfileId = "slow";
        }
        else if (RadioFast.IsChecked == true)
        {
            settings.ActiveProfileId = "fast";
        }
        else
        {
            settings.ActiveProfileId = "average";
        }

        settings.IsFirstRun = false;
        _configService.SaveSettings(settings);

        DialogResult = true;
        Close();
    }
}
