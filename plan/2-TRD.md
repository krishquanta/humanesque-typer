# 2. Technical Requirements Document (TRD) — Humanesque Typer

> **Project**: Humanesque (Humanesque Typer)  
> **Status**: Approved Specification  
> **Version**: 1.0.0  
> **Runtime**: .NET 10 LTS (C# 13) / Windows Desktop  

---

## 1. Functional Requirements

### 1.1 Target Window Acquisition & Management
- **FR-WIN-01**: System SHALL enumerate all visible, non-minimized top-level Windows desktop windows using `EnumWindows` and `GetWindowTextW`.
- **FR-WIN-02**: System SHALL provide an interactive crosshair tool using mouse capture (`SetCapture`, `GetCursorPos`, `WindowFromPoint`) to allow drag-and-release target selection.
- **FR-WIN-03**: System SHALL register a system-wide hotkey (`Ctrl+Shift+T`) via `RegisterHotKey` to lock the current foreground window as the active typing target.
- **FR-WIN-04**: System SHALL resolve the appropriate child edit control when targeting complex nested Win32 applications (e.g. `Edit` in classic Notepad, `RichEditD2DPT` in Modern Notepad, or `_WwG` document canvas in Microsoft Word).

### 1.2 Biomimetic Human Typing Simulation
- **FR-TYPE-01**: System SHALL generate inter-keystroke intervals using a bounded Gaussian distribution:
  $$T_{\text{key}} = \mu_{\text{mode}} + \sigma_{\text{mode}} \cdot Z$$
  where $Z \sim \mathcal{N}(0, 1)$, clamped within $[\text{MinDelay}, \text{MaxDelay}]$.
- **FR-TYPE-02**: System SHALL apply an acceleration multiplier ($0.4\times - 0.7\times$) when processing letters that belong to the Top 1,000 common English words or frequent n-grams (*th, he, in, er, an, re*).
- **FR-TYPE-03**: System SHALL compute realistic spatial typos based on physical Euclidean proximity in a QWERTY matrix.
- **FR-TYPE-04**: When a typo is triggered:
  1. Emit the erroneous neighbor character.
  2. Continue typing 1 to 3 additional letters ("overtype hesitation").
  3. Pause for cognitive reaction time (200–450ms).
  4. Emit the appropriate number of `VK_BACK` (Backspace) keystrokes.
  5. Re-type the intended characters correctly.
- **FR-TYPE-05**: System SHALL inject extended pauses at punctuation marks:
  - Sentence terminators (`.`, `?`, `!`): $400\text{ms} - 1200\text{ms}$.
  - Phrase breaks (`,`, `;`, `:`, `—`): $180\text{ms} - 500\text{ms}$.
  - Paragraph / newline transitions: $800\text{ms} - 2500\text{ms}$.

### 1.3 Smart Dual Injection Dispatcher
- **FR-INJ-01 (Pure Background Mode - Default)**: Keystrokes SHALL be dispatched via Win32 `PostMessageW(hWnd, WM_CHAR, wParam, lParam)` without calling `SetForegroundWindow` or `SetActiveWindow`.
- **FR-INJ-02 (Foreground Fallback Mode)**: For modern Chromium/Electron apps (e.g. Google Docs) that discard background messages, provide an opt-in mode using `SendInput` preceded by a configurable countdown timer (default 5s) and automatic focus restore.
- **FR-INJ-03 (Unicode & Special Keys)**: Support full UTF-16 character injection, including surrogate pairs and emojis, utilizing `WM_UNICHAR` or surrogate pair sequencing.

### 1.4 Global Controls & Multitasking Safety
- **FR-CTL-01**: System SHALL register global hotkey `F8` to toggle Pause and Resume.
- **FR-CTL-02**: System SHALL register global hotkey `F9` (and `Esc`) to immediately terminate the typing session (Emergency Abort).
- **FR-CTL-03**: System SHALL maintain a Windows System Tray icon indicating status (Idle, Typing, Paused) with context menu controls and completion balloon notifications.
- **FR-CTL-04**: System SHALL offer an optional transparent, click-through-capable floating status pill displaying real-time WPM, elapsed time, and progress bar.

### 1.5 Document & Text Ingestion
- **FR-DOC-01**: Provide direct multi-line text input with character/word counters and clear button.
- **FR-DOC-02**: Support drag-and-drop file ingestion for `.txt`, `.md`, and `.docx` (Office Open XML).
- **FR-DOC-03**: Preserve whitespace fidelity: indentation spaces, tab characters (`\t`), line feeds (`\r\n`), and bullet structure.

---

## 2. Non-Functional Requirements (NFRs)

| Category | Requirement | Specification |
| :--- | :--- | :--- |
| **Timer Precision** | High-Resolution Scheduling | Standard `Thread.Sleep` has ~15ms jitter on Windows. System MUST call `timeBeginPeriod(1)` to achieve 1ms timer resolution. |
| **Performance** | Memory & CPU Footprint | Idle CPU: 0.0%; Active typing CPU: < 1.0%; Working Set Memory: < 65 MB. |
| **Reliability** | Exception Isolation | Target window closure during typing MUST be caught cleanly (`ERROR_INVALID_WINDOW_HANDLE`) without crashing the application. |
| **Local-First & Privacy** | Zero Telemetry | All text parsing, typing simulation, and calibration data MUST remain 100% offline and local on the user's machine. |
| **Responsiveness** | Hotkey Latency | Emergency abort (`F9`) and Pause (`F8`) hotkey triggers MUST interrupt the typing thread in $\le 5\text{ms}$. |

---

## 3. Technology Constraints & Stack

- **Runtime & Language**: C# 13 on **.NET 10 LTS** (with backwards-compatible target support for .NET 8 LTS).
- **GUI Framework**: **WPF (Windows Presentation Foundation)** with modern XAML styling and high-DPI awareness (Per-Monitor V2).
- **Architecture Pattern**: MVVM (Model-View-ViewModel) utilizing `CommunityToolkit.Mvvm` for source-generated properties, commands, and messaging.
- **External Dependencies & Packages**:
  - `CommunityToolkit.Mvvm` (v8.3+) – MVVM architecture and decoupled messaging.
  - `DocumentFormat.OpenXml` (v3.1+) – Native, fast `.docx` text extraction without requiring Microsoft Word interop.
  - `Markdig` (v0.37+) – Markdown document parsing and plain-text extraction.
  - `Hardcodet.NotifyIcon.Wpf` (or native Win32 `Shell_NotifyIconW`) – Windows system tray integration.
- **Native Interop (P/Invoke `user32.dll` & `winmm.dll`)**:
  - `user32.dll`: `PostMessageW`, `SendMessageW`, `EnumWindows`, `GetWindowTextW`, `GetClassNameW`, `WindowFromPoint`, `SetCapture`, `ReleaseCapture`, `RegisterHotKey`, `UnregisterHotKey`, `AttachThreadInput`, `GetWindowThreadProcessId`, `SendInput`.
  - `winmm.dll`: `timeBeginPeriod`, `timeEndPeriod` (for 1ms high-precision scheduling).

---

## 4. Integration Points & Win32 P/Invoke Signatures

```csharp
internal static partial class NativeMethods
{
    public const uint WM_CHAR = 0x0102;
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_UNICHAR = 0x0109;
    public const uint UNICODE_NOCHAR = 0xFFFF;

    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint VK_F8 = 0x77;
    public const uint VK_F9 = 0x78;

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "WindowFromPoint")]
    public static partial IntPtr WindowFromPoint(POINT point);

    [LibraryImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    public static partial uint TimeBeginPeriod(uint uPeriod);

    [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    public static partial uint TimeEndPeriod(uint uPeriod);
}
```

---

## 5. Performance Budgets

| Subsystem | Metric | Budget Limit |
| :--- | :--- | :--- |
| Window Picker | Enumeration latency for 50 open windows | $\le 12\text{ms}$ |
| Input Queue Dispatch | Keystroke dispatch cycle time | $\le 1\text{ms}$ |
| Docx File Ingestion | 200-page document text parse | $\le 350\text{ms}$ |
| Cold Application Launch | Time to interactive (Main Window loaded) | $\le 650\text{ms}$ |
