using System.Windows;
using System.Windows.Input;
using Humanesque.Models;

namespace Humanesque.Views;

public partial class FloatingPillWindow : Window
{
    public FloatingPillWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Right - Width - 30;
        Top = SystemParameters.WorkArea.Top + 30;
    }

    public void UpdateProgress(SessionStats stats)
    {
        Dispatcher.Invoke(() =>
        {
            PillProgressBar.Value = stats.ProgressPercentage;
            PillStatusText.Text = $"{stats.CurrentWpm:F0} WPM | {stats.ProgressPercentage:F0}%";
        });
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
