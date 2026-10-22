using System.Runtime.InteropServices;

namespace Humanesque.Native;

public class InputDispatcher
{
    /// <summary>
    /// Dispatches a single character into the target window's message queue in the background.
    /// Does NOT activate the window, change foreground focus, or disrupt user typing in other apps.
    /// </summary>
    public bool PostChar(IntPtr hWnd, char c)
    {
        if (hWnd == IntPtr.Zero) return false;

        // Special handling for Enter (Carriage return / line feed)
        if (c == '\r')
        {
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_RETURN, (IntPtr)0x001C0001);
            Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\r', (IntPtr)0x001C0001);
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_RETURN, unchecked((IntPtr)(long)0xC01C0001));
            return true;
        }

        if (c == '\n')
        {
            // Skip redundant line feed if preceded by \r, otherwise treat as return
            return true;
        }

        // Special handling for Tab
        if (c == '\t')
        {
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_TAB, (IntPtr)0x000F0001);
            Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)'\t', (IntPtr)0x000F0001);
            Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_TAB, unchecked((IntPtr)(long)0xC00F0001));
            return true;
        }

        // Standard character dispatch
        return Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)c, IntPtr.Zero);
    }

    /// <summary>
    /// Dispatches a Backspace keystroke into the target window message queue to erase an erroneous character.
    /// </summary>
    public bool PostBackspace(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return false;

        Win32Api.PostMessage(hWnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_BACK, (IntPtr)0x000E0001);
        Win32Api.PostMessage(hWnd, Win32Api.WM_CHAR, (IntPtr)Win32Api.VK_BACK, (IntPtr)0x000E0001);
        Win32Api.PostMessage(hWnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_BACK, unchecked((IntPtr)(long)0xC00E0001));
        return true;
    }
}
