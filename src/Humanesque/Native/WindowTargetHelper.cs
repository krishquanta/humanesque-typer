using System.Diagnostics;
using System.Text;
using Humanesque.Models;

namespace Humanesque.Native;

public static class WindowTargetHelper
{
    private static readonly HashSet<string> IgnoredProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ShellExperienceHost", "SearchHost", "StartMenuExperienceHost",
        "TextInputHost", "LockApp", "ApplicationFrameHost", "explorer"
    };

    public static List<TargetWindowInfo> GetActiveWindows()
    {
        var windows = new List<TargetWindowInfo>();
        var currentPid = Environment.ProcessId;

        Win32Api.EnumWindows((hWnd, lParam) =>
        {
            if (!Win32Api.IsWindowVisible(hWnd)) return true;

            Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == 0 || pid == currentPid) return true;

            var titleBuilder = new StringBuilder(512);
            Win32Api.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string title = titleBuilder.ToString().Trim();

            if (string.IsNullOrEmpty(title)) return true;

            string processName = "Unknown";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
                // Process terminated or access denied
            }

            if (IgnoredProcesses.Contains(processName) && title == "Program Manager")
            {
                return true;
            }

            var classBuilder = new StringBuilder(256);
            Win32Api.GetClassName(hWnd, classBuilder, classBuilder.Capacity);
            string className = classBuilder.ToString();

            IntPtr editHandle = ResolveEditControl(hWnd, className, processName);

            windows.Add(new TargetWindowInfo
            {
                Handle = hWnd,
                EditControlHandle = editHandle,
                WindowTitle = title,
                ProcessName = processName,
                ClassName = className,
                ProcessId = (int)pid,
                IsModernApp = className.Contains("Windows.UI.Core") || processName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) && className.Contains("Notepad")
            });

            return true;
        }, IntPtr.Zero);

        return windows.OrderBy(w => w.ProcessName).ThenBy(w => w.WindowTitle).ToList();
    }

    public static TargetWindowInfo? FromHandle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;

        Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
        string processName = "Unknown";
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch { }

        var titleBuilder = new StringBuilder(512);
        Win32Api.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);

        var classBuilder = new StringBuilder(256);
        Win32Api.GetClassName(hWnd, classBuilder, classBuilder.Capacity);
        string className = classBuilder.ToString();

        IntPtr editHandle = ResolveEditControl(hWnd, className, processName);

        return new TargetWindowInfo
        {
            Handle = hWnd,
            EditControlHandle = editHandle,
            WindowTitle = titleBuilder.ToString(),
            ProcessName = processName,
            ClassName = className,
            ProcessId = (int)pid,
            IsModernApp = className.Contains("Windows.UI.Core") || (processName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) && className.Contains("Notepad"))
        };
    }

    public static IntPtr ResolveEditControl(IntPtr topHwnd, string className, string processName)
    {
        // 1. Classic Notepad uses "Edit" control
        IntPtr childEdit = Win32Api.FindWindowEx(topHwnd, IntPtr.Zero, "Edit", null);
        if (childEdit != IntPtr.Zero) return childEdit;

        // 2. Modern Windows 11 Notepad uses RichEditD2DPT
        IntPtr richEdit = Win32Api.FindWindowEx(topHwnd, IntPtr.Zero, "RichEditD2DPT", null);
        if (richEdit != IntPtr.Zero) return richEdit;

        // 3. Microsoft Word uses "_WwG" for the active document view pane
        if (processName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase))
        {
            IntPtr wordPane = FindChildRecursive(topHwnd, "_WwG");
            if (wordPane != IntPtr.Zero) return wordPane;
        }

        // 4. Default: target top-level handle directly
        return topHwnd;
    }

    private static IntPtr FindChildRecursive(IntPtr parent, string targetClass)
    {
        IntPtr child = IntPtr.Zero;
        while ((child = Win32Api.FindWindowEx(parent, child, null, null)) != IntPtr.Zero)
        {
            var sb = new StringBuilder(256);
            Win32Api.GetClassName(child, sb, sb.Capacity);
            if (sb.ToString().Equals(targetClass, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            IntPtr deeper = FindChildRecursive(child, targetClass);
            if (deeper != IntPtr.Zero) return deeper;
        }
        return IntPtr.Zero;
    }
}
