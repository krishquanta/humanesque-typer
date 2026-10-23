using System.Diagnostics;
using System.Runtime.InteropServices;
using Humanesque.Models;

namespace Humanesque.Native;

public enum InjectionMode
{
    BackgroundAuto,         // 100% Background (Word COM for Word, Child Window Messages for Notepad/Editors) - DEFAULT
    WordComBackground,      // Explicit Word COM Background
    Win32MessageBackground, // Explicit Win32 Child Message Injection
    FocusSendInput          // Foreground Focus Mode (Only for canvas games/direct input)
}

public class InputDispatcher
{
    private dynamic? _activeWordApp;
    private dynamic? _activeWordSelection;
    private IntPtr _resolvedTargetHwnd = IntPtr.Zero;

    public InjectionMode ActiveMode { get; set; } = InjectionMode.BackgroundAuto;
    public IntPtr ResolvedTargetHwnd => _resolvedTargetHwnd;

    /// <summary>
    /// Prepares the injection channel for the target window before typing begins.
    /// Sets up Word COM or resolves the background child edit control without stealing focus.
    /// </summary>
    public bool PrepareTarget(TargetWindowInfo target)
    {
        _activeWordApp = null;
        _activeWordSelection = null;
        _resolvedTargetHwnd = IntPtr.Zero;

        // If target window is currently minimized, restore it quietly without focus
        if (target.Handle != IntPtr.Zero && Win32Api.IsIconic(target.Handle))
        {
            Win32Api.ShowWindow(target.Handle, Win32Api.SW_SHOWNOACTIVATE);
        }

        // Strategy 1: Microsoft Word COM Background
        if (ActiveMode == InjectionMode.WordComBackground || 
            (ActiveMode == InjectionMode.BackgroundAuto && target.ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                object? comObj = Win32Api.GetActiveComObject("Word.Application");

                // Fallback to AccessibleObjectFromWindow if ROT lookup didn't find Word
                if (comObj == null && target.EditControlHandle != IntPtr.Zero)
                {
                    var iid = Win32Api.IID_IDispatch;
                    int hr = Win32Api.AccessibleObjectFromWindow(target.EditControlHandle, Win32Api.OBJID_NATIVEOM, ref iid, out object nativeObj);
                    if (hr == 0 && nativeObj != null)
                    {
                        dynamic win = nativeObj;
                        comObj = win.Application;
                    }
                }

                if (comObj != null)
                {
                    _activeWordApp = comObj;
                    dynamic word = _activeWordApp;
                    if (word.Documents.Count == 0)
                    {
                        word.Documents.Add();
                    }
                    _activeWordSelection = word.Selection;
                    return true;
                }
            }
            catch
            {
                // Word COM fallback to Win32 message
            }
        }

        // Strategy 2: Win32 Child Edit Message Injection (Notepad, WordPad, Scintilla, etc.)
        if (target.EditControlHandle != IntPtr.Zero && target.EditControlHandle != target.Handle)
        {
            _resolvedTargetHwnd = target.EditControlHandle;
        }
        else
        {
            var (editHwnd, _) = WindowTargetHelper.ResolveEditControl(target.Handle, target.ClassName, target.ProcessName);
            _resolvedTargetHwnd = editHwnd != IntPtr.Zero ? editHwnd : target.Handle;
        }

        return true;
    }

    /// <summary>
    /// Dispatches a single character into the target window using the appropriate injection strategy.
    /// </summary>
    public bool DispatchChar(TargetWindowInfo target, char c)
    {
        if (target.Handle == IntPtr.Zero) return false;

        // Strategy 1: Word COM Background Typing (100% background, zero focus theft)
        if (_activeWordSelection != null)
        {
            try
            {
                if (c == '\r' || c == '\n')
                {
                    _activeWordSelection.TypeParagraph();
                }
                else
                {
                    _activeWordSelection.TypeText(c.ToString());
                }
                return true;
            }
            catch
            {
                _activeWordSelection = null; // Revert to Win32 message
            }
        }

        // Strategy 2: Background Win32 Message (Default for BackgroundAuto & Win32MessageBackground)
        if (ActiveMode == InjectionMode.BackgroundAuto || ActiveMode == InjectionMode.Win32MessageBackground)
        {
            IntPtr destHwnd = _resolvedTargetHwnd != IntPtr.Zero ? _resolvedTargetHwnd : target.Handle;
            return SendCharWin32(destHwnd, c);
        }

        // Strategy 3: Foreground Focus SendInput (Only when user explicitly chooses FocusSendInput)
        return SendInputChar(c);
    }

    /// <summary>
    /// Dispatches a backspace key to delete an erroneous character.
    /// </summary>
    public bool DispatchBackspace(TargetWindowInfo target)
    {
        if (target.Handle == IntPtr.Zero) return false;

        // Strategy 1: Word COM
        if (_activeWordSelection != null)
        {
            try
            {
                _activeWordSelection.TypeBackspace();
                return true;
            }
            catch
            {
                _activeWordSelection = null;
            }
        }

        // Strategy 2: Background Win32 Message
        if (ActiveMode == InjectionMode.BackgroundAuto || ActiveMode == InjectionMode.Win32MessageBackground)
        {
            IntPtr destHwnd = _resolvedTargetHwnd != IntPtr.Zero ? _resolvedTargetHwnd : target.Handle;
            return SendBackspaceWin32(destHwnd);
        }

        // Strategy 3: Focus SendInput
        return SendInputKey(Win32Api.VK_BACK);
    }

    /// <summary>
    /// Dispatches a character directly to a window message queue in the background.
    /// </summary>
    public static bool SendCharWin32(IntPtr hWnd, char c)
    {
        if (c == '\r' || c == '\n')
        {
            Win32Api.SendMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_RETURN, (IntPtr)0x001C0001);
            Win32Api.SendMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\r', (IntPtr)0x001C0001);
            Win32Api.SendMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_RETURN, (IntPtr)unchecked((int)0xC01C0001));
            return true;
        }

        if (c == '\t')
        {
            Win32Api.SendMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_TAB, (IntPtr)0x000F0001);
            Win32Api.SendMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\t', (IntPtr)0x000F0001);
            Win32Api.SendMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_TAB, (IntPtr)unchecked((int)0xC00F0001));
            return true;
        }

        Win32Api.SendMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)c, (IntPtr)1);
        return true;
    }

    /// <summary>
    /// Dispatches a backspace key directly to a window message queue in the background.
    /// </summary>
    public static bool SendBackspaceWin32(IntPtr hWnd)
    {
        Win32Api.SendMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_BACK, (IntPtr)0x000E0001);
        Win32Api.SendMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)0x08, (IntPtr)0x000E0001);
        Win32Api.SendMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_BACK, (IntPtr)unchecked((int)0xC00E0001));
        return true;
    }

    /// <summary>
    /// Injects a Unicode character via Windows SendInput (Foreground mode).
    /// </summary>
    public static bool SendInputChar(char c)
    {
        if (c == '\r' || c == '\n')
        {
            return SendInputKey(Win32Api.VK_RETURN);
        }

        if (c == '\t')
        {
            return SendInputKey(Win32Api.VK_TAB);
        }

        var inputs = new INPUT[2];

        // Key Down (Unicode)
        inputs[0].type = Win32Api.INPUT_KEYBOARD;
        inputs[0].u.ki.wScan = (ushort)c;
        inputs[0].u.ki.dwFlags = Win32Api.KEYEVENTF_UNICODE;

        // Key Up (Unicode)
        inputs[1].type = Win32Api.INPUT_KEYBOARD;
        inputs[1].u.ki.wScan = (ushort)c;
        inputs[1].u.ki.dwFlags = Win32Api.KEYEVENTF_UNICODE | Win32Api.KEYEVENTF_KEYUP;

        uint sent = Win32Api.SendInput((uint)inputs.Length, inputs, INPUT.Size);
        return sent == (uint)inputs.Length;
    }

    /// <summary>
    /// Injects a virtual key (e.g. Backspace, Return) via Windows SendInput (Foreground mode).
    /// </summary>
    public static bool SendInputKey(ushort vkCode)
    {
        var inputs = new INPUT[2];

        // Key Down
        inputs[0].type = Win32Api.INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vkCode;
        inputs[0].u.ki.dwFlags = 0;

        // Key Up
        inputs[1].type = Win32Api.INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = vkCode;
        inputs[1].u.ki.dwFlags = Win32Api.KEYEVENTF_KEYUP;

        uint sent = Win32Api.SendInput((uint)inputs.Length, inputs, INPUT.Size);
        return sent == (uint)inputs.Length;
    }

    public static void ActivateWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;

        if (Win32Api.IsIconic(hWnd))
        {
            Win32Api.ShowWindow(hWnd, Win32Api.SW_RESTORE);
        }

        Win32Api.SetForegroundWindow(hWnd);
    }
}
