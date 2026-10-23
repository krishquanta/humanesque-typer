using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Humanesque.Models;
using Humanesque.Native;
using Humanesque.ViewModels;
using Microsoft.Win32;

namespace Humanesque.Views;

public partial class MainWindow : Window
{
    private readonly GlobalHotkeyManager _hotkeyManager = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hotkeyManager.Initialize(helper.Handle);

        _hotkeyManager.PauseResumeRequested += () =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.TogglePauseResumeCommand.Execute(null);
            }
        };

        _hotkeyManager.AbortRequested += () =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.EmergencyAbortCommand.Execute(null);
            }
        };

        _hotkeyManager.LockTargetRequested += () =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.LockActiveWindow();
            }
        };
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _hotkeyManager.Dispose();
    }

    private void OnLockActiveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.LockActiveWindow();
        }
    }

    private void OnSelectSlowProfile(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ActiveProfile = TypingProfile.CreateSlow();
        }
    }

    private void OnSelectAverageProfile(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ActiveProfile = TypingProfile.CreateAverage();
        }
    }

    private void OnSelectFastProfile(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ActiveProfile = TypingProfile.CreateFast();
        }
    }

    private void OnInjectionModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && ComboMode.SelectedItem is ComboBoxItem item)
        {
            string? tag = item.Tag as string;
            vm.ActiveInjectionMode = tag switch
            {
                "WordComBackground" => InjectionMode.WordComBackground,
                "Win32MessageBackground" => InjectionMode.Win32MessageBackground,
                "FocusSendInput" => InjectionMode.FocusSendInput,
                _ => InjectionMode.BackgroundAuto
            };
        }
    }

    private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private async void OnImportFileClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Supported Documents|*.txt;*.md;*.docx|Word Documents (*.docx)|*.docx|Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Import Document to Type"
        };

        if (dialog.ShowDialog() == true && DataContext is MainViewModel vm)
        {
            await vm.LoadFileAsync(dialog.FileName);
        }
    }

    private void OnClearTextClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.DocumentText = string.Empty;
        }
    }

    private async void OnTextDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && DataContext is MainViewModel vm)
            {
                await vm.LoadFileAsync(files[0]);
            }
        }
    }
}
