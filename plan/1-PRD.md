# 1. Product Requirements Document (PRD) — Humanesque Typer

> **Project**: Humanesque (Humanesque Typer)  
> **Status**: Approved Specification  
> **Version**: 1.0.0  
> **Target OS**: Windows 10 / Windows 11  

---

## 1. Problem Statement
Automated text injection and macro typing tools traditionally suffer from two fatal flaws:
1. **Focus Hijacking**: Conventional autotypers use physical keyboard simulation (`SendInput`), which requires the target window (e.g., Microsoft Word, Notepad, or a web form) to remain active in the foreground. If the user clicks away or Alt+Tabs to multitask, the script either dumps keystrokes into the wrong application or halts completely.
2. **Robotic Uniformity**: Standard text streaming injects characters at fixed, unnatural intervals (e.g., exactly 50ms per character). This creates an immediate machine signature, lacks the natural cadence of human thought, produces no realistic typos, and triggers anti-cheat or automated proctoring surveillance flags.

**Humanesque Typer** solves this by providing a biomimetic background typing engine. It delivers text directly into a designated target window's message queue while the user is actively working in other applications, accurately mimicking human typing rhythms, muscle memory bursts, spatial keyboard typos, and realistic backspace corrections.

---

## 2. Target Personas & Use Cases

| Persona | Scenario & Need | Value Delivered |
| :--- | :--- | :--- |
| **Multitasking Professional** | Needs to populate documents or test input fields in Microsoft Word, Notepad, or internal tools while actively responding to emails or browsing. | Completely non-intrusive background typing without losing keyboard/mouse focus in their active window. |
| **Content Creator & Presenter** | Needs realistic, natural-speed typing recordings or live screen demos without manual typing fatigue or flubbed live scripts. | Lifelike typing cadence with customizable typo rates and configurable pauses between sentences. |
| **QA / Software Tester** | Tests text editors, document processors, or web inputs under realistic human typing loads and varying WPM speeds. | Controlled Slow, Average, and Fast simulation profiles with reproducible error rates. |
| **Accessibility / Ergonomics User** | Users with repetitive strain injury (RSI) who have drafts ready and want them typed smoothly into local applications. | Hands-free background document playback. |

---

## 3. Goals & Non-Goals

### 3.1 Goals
- **Pure Background Typing**: Inject text directly into a target window (e.g., Microsoft Word, Notepad, text editors) without requiring the window to be in the foreground or stealing focus.
- **Biomimetic Human Typing Model**:
  - Variable inter-key delays governed by Gaussian distributions.
  - Common n-gram/word acceleration (muscle-memory bursts on frequent words like *the*, *and*, *with*).
  - Realistic spatial typos based on physical QWERTY keyboard proximity (e.g., hitting `r` or `w` instead of `e`).
  - Realistic multi-keystroke overtype detection followed by Backspace corrections.
  - Natural pauses at punctuation (periods, commas, semicolons) and paragraph breaks.
- **3 Curated Modes + Calibration**:
  - *Slow Typer*: ~25–40 WPM, higher hesitation, higher typo rate.
  - *Average Typer*: ~50–70 WPM, balanced rhythm, standard typo frequency.
  - *Fast Typer*: ~85–115 WPM, rapid bursts, quick snap corrections.
  - *Onboarding Calibration Test*: Optional 15-second typing test during first launch to measure user's natural cadence and auto-calibrate the profiles.
- **Visual Target Selection**:
  - Live dropdown of running top-level windows.
  - "Drag-to-select" crosshair picker tool.
  - Global hotkey (`Ctrl+Shift+T`) to lock the currently focused window as target.
- **Global Multitasking Controls**:
  - Hotkeys: `F8` (Pause/Resume), `F9` or `Esc` (Emergency Abort).
  - System Tray icon with progress percentage and balloon notifications.
  - Optional discreet floating status pill showing live WPM and session progress.
- **Multi-Format Document Ingestion**:
  - Direct paste text area.
  - Drag-and-drop file ingestion for `.txt`, `.md`, and `.docx` files.
  - Full Unicode (UTF-16) character and whitespace fidelity (tabs, indents, newlines).

### 3.2 Non-Goals (v1)
- Keylogging or capturing user keystrokes outside of the optional 15-second calibration test.
- Cross-platform support (v1 is native to Windows 10/11 Win32).
- Optical Character Recognition (OCR) for scanned PDFs or images.

---

## 4. Success Metrics & KPIs

| Metric | Target | Verification Method |
| :--- | :--- | :--- |
| **Zero Focus Interruption** | 100% of background keystrokes delivered without stealing foreground active window state. | Automated test running while actively typing in Notepad. |
| **Cadence Realism** | Inter-key delay distribution matches Gaussian curve with $p < 0.05$ variance; burst speeds on top 100 English n-grams. | Automated timing telemetry benchmark. |
| **Typo Proximity Accuracy** | 100% of simulated typos originate from adjacent keys on standard QWERTY layout. | Unit tests on `KeyboardLayoutMatrix`. |
| **Document Ingestion Speed** | < 500ms to parse and prepare a 50,000-word `.docx` or `.txt` file. | Performance benchmark on large sample files. |
| **Resource Efficiency** | < 60 MB RAM footprint; 0% CPU consumption while paused or idle. | Windows Task Manager / Diagnostics Tools. |

---

## 5. Feature Inventory & Priorities

| ID | Feature | Description | Priority |
| :--- | :--- | :--- | :---: |
| **FEAT-01** | Window Targeting Suite | Live window dropdown + Crosshair drag picker + `Ctrl+Shift+T` hotkey lock. | **P0** |
| **FEAT-02** | Pure Background Injection | Win32 `PostMessageW` (`WM_CHAR` / `WM_UNICHAR`) background dispatcher. | **P0** |
| **FEAT-03** | Biomimetic Cadence Engine | Gaussian key delays, punctuation pauses, thinking gaps, n-gram burst typing. | **P0** |
| **FEAT-04** | QWERTY Typo & Correction | Spatial neighbor typos, 1–3 char overtype lag, realistic backspacing. | **P0** |
| **FEAT-05** | 3 Preset Modes | Slow (~30 WPM), Average (~60 WPM), Fast (~95 WPM) with custom sliders. | **P0** |
| **FEAT-06** | Multitasking Safety Controls | Global hotkeys (`F8` Pause/Resume, `F9` Abort) + System Tray icon. | **P0** |
| **FEAT-07** | Text & File Ingestion | Direct text area, drag-and-drop `.txt`, `.md`, `.docx` parser with Unicode support. | **P1** |
| **FEAT-08** | Onboarding Setup Wizard | First-run setup modal with profile selector and optional 15s calibration test. | **P1** |
| **FEAT-09** | Floating Status Pill | Discreet draggable overlay with live WPM, progress %, and pause toggle. | **P1** |
| **FEAT-10** | Smart Dual Injection Fallback | Optional `SendInput` mode with 5s countdown for apps that reject `WM_CHAR`. | **P2** |
