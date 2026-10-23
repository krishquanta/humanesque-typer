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

            var (editHandle, editClassName) = ResolveEditControl(hWnd, className, processName);

            windows.Add(new TargetWindowInfo
            {
                Handle = hWnd,
                EditControlHandle = editHandle,
                EditControlClassName = editClassName,
                WindowTitle = title,
                ProcessName = processName,
                ClassName = className,
                ProcessId = (int)pid,
                IsModernApp = className.Contains("Windows.UI.Core") || (processName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) && className.Contains("Notepad"))
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

        var (editHandle, editClassName) = ResolveEditControl(hWnd, className, processName);

        return new TargetWindowInfo
        {
            Handle = hWnd,
            EditControlHandle = editHandle,
            EditControlClassName = editClassName,
            WindowTitle = titleBuilder.ToString(),
            ProcessName = processName,
            ClassName = className,
            ProcessId = (int)pid,
            IsModernApp = className.Contains("Windows.UI.Core") || (processName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) && className.Contains("Notepad"))
        };
    }

    public static (IntPtr EditHandle, string EditClassName) ResolveEditControl(IntPtr topHwnd, string className, string processName)
    {
        if (topHwnd == IntPtr.Zero) return (IntPtr.Zero, string.Empty);

        // 1. If WINWORD, check for Word document pane "_WwG"
        if (processName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase))
        {
            IntPtr wordPane = IntPtr.Zero;
            string wordCls = string.Empty;
            Win32Api.EnumChildWindows(topHwnd, (child, _) =>
            {
                var sb = new StringBuilder(256);
                Win32Api.GetClassName(child, sb, sb.Capacity);
                string c = sb.ToString();
                if (c.Equals("_WwG", StringComparison.OrdinalIgnoreCase))
                {
                    wordPane = child;
                    wordCls = c;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (wordPane != IntPtr.Zero) return (wordPane, wordCls);
        }

        // 2. Enumerate all descendant controls to find rich edit, plain edit, scintilla, etc.
        var candidates = new List<(IntPtr Hwnd, string ClassName, int Priority, bool Visible)>();

        Win32Api.EnumChildWindows(topHwnd, (child, _) =>
        {
            var sb = new StringBuilder(256);
            Win32Api.GetClassName(child, sb, sb.Capacity);
            string cls = sb.ToString();
            bool visible = Win32Api.IsWindowVisible(child);

            if (cls.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 100, visible));
            else if (cls.Equals("Edit", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 90, visible));
            else if (cls.Contains("RichEdit", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 85, visible));
            else if (cls.Contains("Scintilla", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 80, visible));
            else if (cls.Equals("_WwG", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 75, visible));
            else if (cls.Contains("RenderWidgetHost", StringComparison.OrdinalIgnoreCase))
                candidates.Add((child, cls, 60, visible));

            return true;
        }, IntPtr.Zero);

        if (candidates.Count > 0)
        {
            var best = candidates.OrderByDescending(c => c.Visible).ThenByDescending(c => c.Priority).First();
            return (best.Hwnd, best.ClassName);
        }

        // 3. Fallback: query focused child window via thread attachment
        uint tid = Win32Api.GetWindowThreadProcessId(topHwnd, out _);
        if (tid != 0)
        {
            uint curTid = Win32Api.GetCurrentThreadId();
            if (Win32Api.AttachThreadInput(curTid, tid, true))
            {
                try
                {
                    IntPtr focus = Win32Api.GetFocus();
                    if (focus != IntPtr.Zero && focus != topHwnd)
                    {
                        var sb = new StringBuilder(256);
                        Win32Api.GetClassName(focus, sb, sb.Capacity);
                        return (focus, sb.ToString());
                    }
                }
                finally
                {
                    Win32Api.AttachThreadInput(curTid, tid, false);
                }
            }
        }

        return (topHwnd, className);
    }
}
