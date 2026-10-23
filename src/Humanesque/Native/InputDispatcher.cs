using System.Diagnostics;
using System.Runtime.InteropServices;
using Humanesque.Models;

namespace Humanesque.Native;

public enum InjectionMode
{
    AutoDetect,
    WordComBackground,
    FocusSendInput,
    Win32Message
}

public class InputDispatcher
{
    private dynamic? _activeWordApp;
    private dynamic? _activeWordSelection;

    public InjectionMode ActiveMode { get; set; } = InjectionMode.AutoDetect;

    /// <summary>
    /// Prepares the injection channel for the target window before typing begins.
    /// Connects to Word COM if target is WINWORD, or ensures window focus if FocusSendInput is used.
    /// </summary>
    public bool PrepareTarget(TargetWindowInfo target)
    {
        _activeWordApp = null;
        _activeWordSelection = null;

        if (ActiveMode == InjectionMode.WordComBackground || 
            (ActiveMode == InjectionMode.AutoDetect && target.ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                object? comObj = Win32Api.GetActiveComObject("Word.Application");
                if (comObj != null)
                {
                    _activeWordApp = comObj;
                    _activeWordSelection = _activeWordApp.Selection;
                    return true;
                }
            }
            catch
            {
                // Fall back to FocusSendInput if Word COM fails
            }
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
                _activeWordSelection = null; // Revert to alternative
            }
        }

        // Strategy 2: Focus SendInput (Universal for Word, Notepad, Chrome, VS Code, etc.)
        if (ActiveMode == InjectionMode.FocusSendInput || ActiveMode == InjectionMode.AutoDetect)
        {
            return SendInputChar(c);
        }

        // Strategy 3: Win32 Message Queue (PostMessage / SendMessage)
        IntPtr targetHwnd = target.EditControlHandle != IntPtr.Zero ? target.EditControlHandle : target.Handle;
        return PostCharWin32(targetHwnd, c);
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

        // Strategy 2: SendInput
        if (ActiveMode == InjectionMode.FocusSendInput || ActiveMode == InjectionMode.AutoDetect)
        {
            return SendInputKey(Win32Api.VK_BACK);
        }

        // Strategy 3: Win32 Message
        IntPtr targetHwnd = target.EditControlHandle != IntPtr.Zero ? target.EditControlHandle : target.Handle;
        return PostBackspaceWin32(targetHwnd);
    }

    /// <summary>
    /// Injects a Unicode character via Windows SendInput.
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
    /// Injects a virtual key (e.g. Backspace, Return) via Windows SendInput.
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

    private static bool PostCharWin32(IntPtr hWnd, char c)
    {
        if (c == '\r')
        {
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_RETURN, (IntPtr)1);
            Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\r', (IntPtr)1);
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_RETURN, (IntPtr)1);
            return true;
        }

        if (c == '\n') return true;

        if (c == '\t')
        {
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_TAB, (IntPtr)1);
            Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\t', (IntPtr)1);
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_TAB, (IntPtr)1);
            return true;
        }

        return Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)c, (IntPtr)1);
    }

    private static bool PostBackspaceWin32(IntPtr hWnd)
    {
        Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_BACK, (IntPtr)1);
        Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)Win32Api.VK_BACK, (IntPtr)1);
        Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_BACK, (IntPtr)1);
        return true;
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
