# Humanesque Typer ⌨️✨

> **Biomimetic Background Autotyper for Windows**  
> Types text into any target window (Microsoft Word, Notepad, document editors) **fully in the background**, allowing you to Alt+Tab away and do other work uninterrupted.

---

## 🌟 Key Features

- **Zero-Focus Background Delivery**: Uses Win32 `PostMessageW(hWnd, WM_CHAR)` to inject keystrokes directly into the target window's message queue without activating it or stealing your keyboard focus.
- **Biomimetic Human Cadence**:
  - **Gaussian Inter-Key Delays**: Natural timing variations modeled with Box-Muller Gaussian distributions.
  - **Spatial QWERTY Typos**: Simulated typos strike physical adjacent keys (e.g. `r` or `w` instead of `e`).
  - **Cognitive Overtype & Backspace Correction**: Types 1–3 additional characters before human hesitation kicks in, followed by rapid backspaces and re-typing.
  - **Muscle-Memory Burst Acceleration**: Common English digraphs and words (*the, and, with, that*) type up to 45% faster.
  - **Contextual Pauses**: Extended thinking delays at periods, commas, colons, and paragraph breaks.
- **3 Speed Profiles + Onboarding Calibration**:
  - 🐢 **Slow Typer**: ~30 WPM, higher hesitation, higher typo rate.
  - 🚶 **Average Typer**: ~60 WPM, natural conversational pace.
  - 🏎️ **Fast Typer**: ~95 WPM, snappy burst touch-typing.
  - 🎯 **15-Second Calibration Test**: Measures your personal WPM and error rate during first-time launch to generate an auto-tuned profile.
- **Visual Target Selection**:
  - Live window dropdown list of running desktop applications.
  - Instant hotkey lock (`Ctrl+Shift+T`) to register the active window.
- **Global Multitasking Controls**:
  - `F8` $\rightarrow$ **Pause / Resume** typing at any time from any app.
  - `F9` or `Esc` $\rightarrow$ **Emergency Abort**.
  - `Ctrl+Shift+T` $\rightarrow$ **Lock Target Window**.
  - Discreet floating status pill showing live progress and WPM.
- **Multi-Format Ingestion**:
  - Direct paste text area.
  - Drag-and-drop support for `.txt`, `.md` (Markdown), and `.docx` (Office Open XML).

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 or Windows 11
- .NET 10 LTS SDK (installed at `$env:USERPROFILE\.dotnet`)

### Running the Application
```powershell
# Run Humanesque Typer
dotnet run --project src/Humanesque/Humanesque.csproj
```

### Running the Test Suite
```powershell
# Run all 23 unit and integration tests
dotnet test Humanesque.slnx
```

---

## 📁 Repository Structure
```
d:\slop-code\humanesque-typer\
├── plan\                                 # 6-Step Specification Framework
│   ├── 1-PRD.md                          # Product Requirements Document
│   ├── 2-TRD.md                          # Technical Requirements Document
│   ├── 3-Architecture.md                 # System Architecture Design
│   ├── 4-UIUX.md                         # UI/UX Specifications & Wireframes
│   ├── 5-SDD.md                          # Software Design Document & Algorithms
│   └── 6-Implementation-Plan.md          # Milestones & Verification Plan
├── src\
│   └── Humanesque\
│       ├── App.xaml                      # App Entry & Dark Theme Resources
│       ├── Core\                         # Biomimetic Cadence, Typos & Engine
│       ├── Native\                       # Win32 P/Invoke, PostMessage & Timers
│       ├── Models\                       # Profiles, WindowInfo & Telemetry
│       ├── Services\                     # Document Ingestion, Config & Calibration
│       ├── ViewModels\                   # MVVM Presentation ViewModels
│       └── Views\                        # WPF Windows & Dialogs
└── tests\
    └── Humanesque.Tests\                 # xUnit Test Suite (23 Passing Tests)
```

---

## 🛡️ License
Private and confidential. Built for pair programming with Antigravity.
