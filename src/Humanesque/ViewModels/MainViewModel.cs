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
    private string _documentText = "Welcome to Humanesque Typer! Select your target window (like Microsoft Word or Notepad), choose a typing speed, and click Start. The text will type with genuine human cadence, muscle-memory word bursts, and natural typo corrections.";

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
    private string _statusMessage = "Ready — Select a target window and press Start Typing";

    [ObservableProperty]
    private string _statusColor = "#10B981"; // StatusSuccess green

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _canStart = true;

    [ObservableProperty]
    private string _wordCountText = "0 words | 0 characters";

    [ObservableProperty]
    private InjectionMode _activeInjectionMode = InjectionMode.AutoDetect;

    public MainViewModel()
    {
        _engine.ProgressUpdated += OnProgressUpdated;
        _engine.StateChanged += OnEngineStateChanged;
        _engine.CountdownTick += OnCountdownTick;

        LoadProfiles();
        RefreshWindows();
        UpdateWordCount();
    }

    partial void OnDocumentTextChanged(string value)
    {
        UpdateWordCount();
    }

    partial void OnSelectedWindowChanged(TargetWindowInfo? value)
    {
        if (value != null)
        {
            if (value.ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Target: {value.DisplayText} (Direct Word COM Background Typing Available)";
                StatusColor = "#818CF8";
            }
            else
            {
                StatusMessage = $"Target: {value.DisplayText} (Ready)";
                StatusColor = "#10B981";
            }
        }
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
            StatusColor = "#F59E0B";
            DocumentText = await _docService.LoadDocumentAsync(filePath);
            StatusMessage = $"Loaded {Path.GetFileName(filePath)} ({DocumentText.Length:N0} characters).";
            StatusColor = "#10B981";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            StatusColor = "#EF4444";
        }
    }

    [RelayCommand]
    public async Task StartTypingAsync()
    {
        if (SelectedWindow == null)
        {
            StatusMessage = "⚠️ Please select a target window from the dropdown first!";
            StatusColor = "#EF4444";
            return;
        }

        if (string.IsNullOrWhiteSpace(DocumentText))
        {
            StatusMessage = "⚠️ Please enter or import text to type.";
            StatusColor = "#EF4444";
            return;
        }

        CanStart = false;
        IsRunning = true;
        IsPaused = false;
        _engine.Dispatcher.ActiveMode = ActiveInjectionMode;

        try
        {
            await _engine.StartTypingAsync(DocumentText, SelectedWindow, ActiveProfile, countdownSeconds: 2);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Typing error: {ex.Message}";
            StatusColor = "#EF4444";
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
            if (info != null && !info.ProcessName.Equals(Environment.ProcessPath != null ? Path.GetFileNameWithoutExtension(Environment.ProcessPath) : "Humanesque", StringComparison.OrdinalIgnoreCase))
            {
                SelectedWindow = info;
                StatusMessage = $"🔒 Locked target window: {info.DisplayText}";
                StatusColor = "#818CF8";
            }
        }
    }

    private void OnCountdownTick(int secondsRemaining)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = $"⏳ Starting in {secondsRemaining}s... Position your cursor in {SelectedWindow?.ProcessName}";
            StatusColor = "#F59E0B";
        });
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
                case TypingEngineState.CountingDown:
                    IsRunning = true;
                    IsPaused = false;
                    CanStart = false;
                    break;
                case TypingEngineState.Running:
                    IsRunning = true;
                    IsPaused = false;
                    CanStart = false;
                    StatusMessage = $"▶ Typing into {SelectedWindow?.ProcessName}... (F8=Pause, F9=Stop)";
                    StatusColor = "#10B981";
                    break;
                case TypingEngineState.Paused:
                    IsRunning = true;
                    IsPaused = true;
                    CanStart = false;
                    StatusMessage = "⏸ Typing paused (Press F8 to resume)";
                    StatusColor = "#F59E0B";
                    break;
                case TypingEngineState.Completed:
                    IsRunning = false;
                    IsPaused = false;
                    CanStart = true;
                    StatusMessage = $"✅ Completed: {Stats.CharactersTyped} characters typed successfully!";
                    StatusColor = "#10B981";
                    break;
                case TypingEngineState.Aborted:
                    IsRunning = false;
                    IsPaused = false;
                    CanStart = true;
                    StatusMessage = "⏹ Typing stopped by user.";
                    StatusColor = "#EF4444";
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
