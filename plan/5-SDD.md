# 5. Software Design Document (SDD) — Humanesque Typer

> **Project**: Humanesque (Humanesque Typer)  
> **Status**: Approved Specification  
> **Version**: 1.0.0  
> **Language & Framework**: C# 13, .NET 10 LTS, WPF  

---

## 1. Repository & Project Structure

```
d:\slop-code\humanesque-typer\
├── Humanesque.sln                          # Master Visual Studio / dotnet Solution
├── src\
│   └── Humanesque\
│       ├── Humanesque.csproj               # WPF Project File (.NET 10.0-windows)
│       ├── App.xaml                        # Application Entry Point & Global Resources
│       ├── App.xaml.cs                     # Startup, Single-Instance Guard, Tray Lifecycle
│       │
│       ├── Core\                           # Domain Typing Simulation Engine
│       │   ├── TypingEngine.cs             # Background Task Orchestrator & State Loop
│       │   ├── BiomimeticCadence.cs        # Gaussian Jitter & Inter-Key Delay Calculator
│       │   ├── SpatialTypoEngine.cs        # QWERTY Key Distance & Typo Candidate Generator
│       │   ├── KeyboardLayoutMatrix.cs     # Physical 2D QWERTY Key Coordinate Maps
│       │   ├── OvertypeStateMachine.cs     # Multi-char Overtype & Backspace Coordinator
│       │   └── NGramBurstDetector.cs       # Common English N-grams & Burst Acceleration
│       │
│       ├── Native\                         # Windows Win32 P/Invoke & OS Interop
│       │   ├── Win32Api.cs                 # NativeMethods P/Invoke declarations
│       │   ├── WindowTargetHelper.cs       # EnumWindows, FindWindowEx, Child Resolution
│       │   ├── CrosshairPicker.cs          # SetCapture & WindowFromPoint Drag Listener
│       │   ├── InputDispatcher.cs          # Dual Dispatcher (PostMessageW vs SendInput)
│       │   ├── HighResolutionTimer.cs      # winmm timeBeginPeriod(1) High-Precision Wait
│       │   └── GlobalHotkeyManager.cs      # RegisterHotKey WndProc Message Router
│       │
│       ├── Models\                         # Canonical Data Transfer & State Objects
│       │   ├── TypingProfile.cs            # Slow / Average / Fast / Custom Parameters
│       │   ├── TargetWindowInfo.cs         # Window Handle, Title, PID, Class
│       │   ├── TypoEvent.cs                # Typo telemetry record (intended vs actual)
│       │   ├── SessionStats.cs             # Live Progress, WPM, Elapsed Time
│       │   └── AppSettings.cs              # Persistent User Configuration Schema
│       │
│       ├── Services\                       # Application Services
│       │   ├── DocumentIngestionService.cs # .txt, .md, .docx text extractors
│       │   ├── ConfigurationService.cs     # Local JSON settings persistence
│       │   ├── ProfileManager.cs           # Profile CRUD & Default Fallbacks
│       │   ├── CalibrationService.cs       # 15s Typing Test Benchmark & Calibrator
│       │   └── SystemTrayManager.cs        # NotifyIcon, Balloon Alerts, Context Menu
│       │
│       ├── ViewModels\                     # MVVM Presentation Logic (CommunityToolkit.Mvvm)
│       │   ├── MainViewModel.cs            # Primary Application ViewModel
│       │   ├── SettingsViewModel.cs        # Tuning Sliders & Profile ViewModel
│       │   ├── OnboardingViewModel.cs      # First-Run Wizard & Typing Test ViewModel
│       │   └── FloatingPillViewModel.cs    # Minimalist Overlay ViewModel
│       │
│       └── Views\                          # XAML Windows & UserControls
│           ├── MainWindow.xaml             # Main Host Window
│           ├── MainWindow.xaml.cs
│           ├── SettingsWindow.xaml         # Configuration Modal
│           ├── SettingsWindow.xaml.cs
│           ├── OnboardingWindow.xaml       # First-Run Wizard
│           ├── OnboardingWindow.xaml.cs
│           ├── FloatingPillWindow.xaml     # Discreet Topmost Overlay
│           ├── FloatingPillWindow.xaml.cs
│           └── Controls\                   # Reusable Controls
│               ├── TargetPickerBar.xaml    # Window Dropdown & Crosshair Tool
│               ├── DocumentEditor.xaml     # Text Area & File Drop Handler
│               └── TelemetryDisplay.xaml   # Progress, WPM, and Counters
│
└── tests\
    └── Humanesque.Tests\
        ├── Humanesque.Tests.csproj         # xUnit Test Project
        ├── CadenceTests.cs                 # Gaussian Variance & Delay Benchmarks
        ├── TypoEngineTests.cs              # QWERTY Proximity & Correction State Tests
        ├── DocumentParserTests.cs          # .txt, .md, .docx parser fidelity
        └── Win32InteropTests.cs            # P/Invoke signature and handle sanity tests
```

---

## 2. Key Module & Algorithm Design

### 2.1 Spatial Typo Engine (`SpatialTypoEngine.cs`)
Human typos are not random characters; they are spatially correlated with neighboring keys on physical keyboards.
The `KeyboardLayoutMatrix` maps standard QWERTY layout into Cartesian coordinates $(x, y)$:

```csharp
public static class KeyboardLayoutMatrix
{
    private static readonly Dictionary<char, (double X, double Y)> KeyCoords = new()
    {
        ['q'] = (0.0, 0.0), ['w'] = (1.0, 0.0), ['e'] = (2.0, 0.0), ['r'] = (3.0, 0.0), ['t'] = (4.0, 0.0),
        ['y'] = (5.0, 0.0), ['u'] = (6.0, 0.0), ['i'] = (7.0, 0.0), ['o'] = (8.0, 0.0), ['p'] = (9.0, 0.0),
        ['a'] = (0.3, 1.0), ['s'] = (1.3, 1.0), ['d'] = (2.3, 1.0), ['f'] = (3.3, 1.0), ['g'] = (4.3, 1.0),
        ['h'] = (5.3, 1.0), ['j'] = (6.3, 1.0), ['k'] = (7.3, 1.0), ['l'] = (8.3, 1.0),
        ['z'] = (0.8, 2.0), ['x'] = (1.8, 2.0), ['c'] = (2.8, 2.0), ['v'] = (3.8, 2.0), ['b'] = (4.8, 2.0),
        ['n'] = (5.8, 2.0), ['m'] = (6.8, 2.0)
    };

    public static char GetSpatialNeighbor(char key, Random rng)
    {
        char lower = char.ToLowerInvariant(key);
        if (!KeyCoords.TryGetValue(lower, out var target)) return key;

        var neighbors = KeyCoords
            .Where(k => k.Key != lower)
            .Select(k => new {
                Key = k.Key,
                Distance = Math.Sqrt(Math.Pow(k.Value.X - target.X, 2) + Math.Pow(k.Value.Y - target.Y, 2))
            })
            .Where(k => k.Distance <= 1.45) // Adjacent physical radius
            .OrderBy(k => k.Distance)
            .ToList();

        if (neighbors.Count == 0) return key;
        char picked = neighbors[rng.Next(neighbors.Count)].Key;
        return char.IsUpper(key) ? char.ToUpperInvariant(picked) : picked;
    }
}
```

### 2.2 Biomimetic Cadence Generator (`BiomimeticCadence.cs`)
Human inter-key delay uses Box-Muller transform to sample from a Gaussian distribution:

$$Z_0 = \sqrt{-2 \ln U_1} \cdot \cos(2\pi U_2)$$
$$\text{Delay} = \text{MeanDelay} + Z_0 \cdot \text{StdDev}$$

Where:
- $\text{MeanDelay} = \frac{60000}{\text{TargetWPM} \times 5}$ ms per character.
- $\text{StdDev} = \text{MeanDelay} \times 0.28$.
- Clamped strictly between $30\text{ms}$ and $350\text{ms}$ for non-punctuation keys.
- **Punctuation Modifiers**:
  - `.` / `!` / `?` $\rightarrow \text{Delay} \times 3.5$
  - `,` / `;` / `:` $\rightarrow \text{Delay} \times 2.0$
  - `\n` $\rightarrow \text{Delay} \times 5.0$
- **N-Gram Burst Acceleration**: If the current 2–3 character sequence is in the high-frequency digraph set (*th, er, on, an, re, he, in, ed, nd*), apply a $0.55\times$ speed factor.

### 2.3 Pure Background Dispatcher (`InputDispatcher.cs`)
Dispatches directly to the target window's message queue:

```csharp
public class InputDispatcher
{
    public void DispatchChar(IntPtr hWnd, char c)
    {
        if (hWnd == IntPtr.Zero) return;

        // PostMessageW dispatches to background queue without stealing focus
        if (c == '\r')
        {
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x0D, IntPtr.Zero);
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_CHAR, (IntPtr)'\r', IntPtr.Zero);
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)0x0D, IntPtr.Zero);
        }
        else if (c == '\b')
        {
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYDOWN, (IntPtr)0x08, IntPtr.Zero);
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_CHAR, (IntPtr)'\b', IntPtr.Zero);
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_KEYUP, (IntPtr)0x08, IntPtr.Zero);
        }
        else
        {
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_CHAR, (IntPtr)c, IntPtr.Zero);
        }
    }
}
```

---

## 3. State Management & Local Persistence

All configuration is persisted to local JSON at `%APPDATA%\Humanesque\settings.json`:

```json
{
  "activeProfileId": "average",
  "profiles": [
    {
      "id": "slow",
      "name": "Slow Typer",
      "targetWpm": 32.0,
      "typoRatePercent": 3.2,
      "minOvertypeCount": 1,
      "maxOvertypeCount": 3,
      "reactionDelayMinMs": 300,
      "reactionDelayMaxMs": 600
    },
    {
      "id": "average",
      "name": "Average Typer",
      "targetWpm": 62.0,
      "typoRatePercent": 1.6,
      "minOvertypeCount": 1,
      "maxOvertypeCount": 2,
      "reactionDelayMinMs": 220,
      "reactionDelayMaxMs": 450
    },
    {
      "id": "fast",
      "name": "Fast Typer",
      "targetWpm": 96.0,
      "typoRatePercent": 0.8,
      "minOvertypeCount": 1,
      "maxOvertypeCount": 2,
      "reactionDelayMinMs": 150,
      "reactionDelayMaxMs": 320
    }
  ],
  "isFirstRun": false,
  "showFloatingPill": true,
  "playCompletionSound": true,
  "hotkeys": {
    "pauseResume": "F8",
    "emergencyAbort": "F9",
    "lockTarget": "Ctrl+Shift+T"
  }
}
```
