using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanesque.Core;
using Humanesque.Models;
using Humanesque.Native;
using Humanesque.Services;

namespace Humanesque.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TypingEngine _engine = new();
    private readonly DocumentIngestionService _docService = new();
    private readonly ConfigurationService _configService = new();

    [ObservableProperty]
    private string _documentText = "Welcome to Humanesque Typer! Select a target window, choose a typing profile, and press Start. You can Alt+Tab away and do other work while the text is typed in the background with human-like cadence, realistic typos, and natural pauses.";

    [ObservableProperty]
    private TargetWindowInfo? _selectedWindow;

    [ObservableProperty]
    private ObservableCollection<TargetWindowInfo> _availableWindows = new();

    [ObservableProperty]
    private TypingProfile _activeProfile = TypingProfile.CreateAverage();

    [ObservableProperty]
    private ObservableCollection<TypingProfile> _profiles = new();

    [ObservableProperty]
    private SessionStats _stats = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private string _wordCountText = "0 words | 0 chars";

    public MainViewModel()
    {
        _engine.ProgressUpdated += OnProgressUpdated;
        _engine.StateChanged += OnEngineStateChanged;

        LoadProfiles();
        RefreshWindows();
        UpdateWordCount();
    }

    partial void OnDocumentTextChanged(string value)
    {
        UpdateWordCount();
    }

    private void UpdateWordCount()
    {
        if (string.IsNullOrWhiteSpace(DocumentText))
        {
            WordCountText = "0 words | 0 characters";
            return;
        }

        int words = DocumentText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        int chars = DocumentText.Length;
        WordCountText = $"{words:N0} words | {chars:N0} characters";
    }

    [RelayCommand]
    public void RefreshWindows()
    {
        AvailableWindows.Clear();
        var list = WindowTargetHelper.GetActiveWindows();
        foreach (var w in list)
        {
            AvailableWindows.Add(w);
        }

        if (SelectedWindow == null && AvailableWindows.Count > 0)
        {
            SelectedWindow = AvailableWindows.FirstOrDefault(w =>
                w.ProcessName.Contains("word", StringComparison.OrdinalIgnoreCase) ||
                w.ProcessName.Contains("notepad", StringComparison.OrdinalIgnoreCase)
            ) ?? AvailableWindows[0];
        }
    }

    [RelayCommand]
    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            StatusMessage = $"Loading {Path.GetFileName(filePath)}...";
            DocumentText = await _docService.LoadDocumentAsync(filePath);
            StatusMessage = $"Loaded {Path.GetFileName(filePath)} successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task StartTypingAsync()
    {
        if (SelectedWindow == null)
        {
            StatusMessage = "Please select a target window first!";
            return;
        }

        if (string.IsNullOrWhiteSpace(DocumentText))
        {
            StatusMessage = "Please enter or import text to type.";
            return;
        }

        IntPtr targetHandle = SelectedWindow.EditControlHandle != IntPtr.Zero
            ? SelectedWindow.EditControlHandle
            : SelectedWindow.Handle;

        CanStart = false;
        IsRunning = true;
        IsPaused = false;
        StatusMessage = $"Typing into {SelectedWindow.DisplayText}...";

        try
        {
            await _engine.StartTypingAsync(DocumentText, targetHandle, ActiveProfile);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Typing error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void TogglePauseResume()
    {
        if (_engine.State == TypingEngineState.Running)
        {
            _engine.Pause();
        }
        else if (_engine.State == TypingEngineState.Paused)
        {
            _engine.Resume();
        }
    }

    [RelayCommand]
    public void EmergencyAbort()
    {
        _engine.Abort();
    }

    public void LockActiveWindow()
    {
        IntPtr fg = Win32Api.GetForegroundWindow();
        if (fg != IntPtr.Zero)
        {
            var info = WindowTargetHelper.FromHandle(fg);
            if (info != null)
            {
                SelectedWindow = info;
                StatusMessage = $"Locked target window: {info.DisplayText}";
            }
        }
    }

    private void OnProgressUpdated(SessionStats updatedStats)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Stats = updatedStats;
        });
    }

    private void OnEngineStateChanged(TypingEngineState state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            switch (state)
            {
                case TypingEngineState.Running:
                    IsRunning = true;
                    IsPaused = false;
                    CanStart = false;
                    StatusMessage = "Typing in progress...";
                    break;
                case TypingEngineState.Paused:
                    IsRunning = true;
                    IsPaused = true;
                    CanStart = false;
                    StatusMessage = "Typing paused (Press F8 to resume)";
                    break;
                case TypingEngineState.Completed:
                    IsRunning = false;
                    IsPaused = false;
                    CanStart = true;
                    StatusMessage = "Typing session completed!";
                    break;
                case TypingEngineState.Aborted:
                    IsRunning = false;
                    IsPaused = false;
                    CanStart = true;
                    StatusMessage = "Typing aborted by user.";
                    break;
                default:
                    IsRunning = false;
                    IsPaused = false;
                    CanStart = true;
                    break;
            }
        });
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        Profiles.Add(TypingProfile.CreateSlow());
        Profiles.Add(TypingProfile.CreateAverage());
        Profiles.Add(TypingProfile.CreateFast());

        var settings = _configService.LoadSettings();
        ActiveProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId) ?? Profiles[1];
    }
}
