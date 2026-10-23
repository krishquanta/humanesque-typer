using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using Humanesque.Native;
using Xunit;
using Xunit.Abstractions;

namespace Humanesque.Tests;

[Collection("NotepadSequential")]
public class UiaTests
{
    private readonly ITestOutputHelper _output;

    public UiaTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(
        IntPtr hwnd, 
        uint dwId, 
        ref Guid riid, 
        [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);

    private const uint OBJID_NATIVEOM = 0xFFFFFFF0;
    private static readonly Guid IID_IDispatch = new("{00020400-0000-0000-C000-000000000046}");

    [Fact]
    public void TestNotepadWindowHierarchyAndBackgroundTyping()
    {
        var proc = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
        Assert.NotNull(proc);

        try
        {
            Thread.Sleep(1500);

            IntPtr validTopHwnd = IntPtr.Zero;

            foreach (var np in Process.GetProcessesByName("notepad"))
            {
                Win32Api.EnumWindows((hWnd, lParam) =>
                {
                    Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == np.Id && Win32Api.IsWindowVisible(hWnd))
                    {
                        var sbTitle = new StringBuilder(256);
                        Win32Api.GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                        var sbClass = new StringBuilder(256);
                        Win32Api.GetClassName(hWnd, sbClass, sbClass.Capacity);

                        if (sbTitle.Length > 0 && !sbClass.ToString().Contains("InputIndicator"))
                        {
                            validTopHwnd = hWnd;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            Assert.True(validTopHwnd != IntPtr.Zero, "Should find a visible Notepad window");

            var childWindows = new List<(IntPtr Hwnd, string ClassName, string Title)>();
            EnumChildWindows(validTopHwnd, (childHwnd, lParam) =>
            {
                var sbClass = new StringBuilder(256);
                Win32Api.GetClassName(childHwnd, sbClass, sbClass.Capacity);
                var sbTitle = new StringBuilder(256);
                Win32Api.GetWindowText(childHwnd, sbTitle, sbTitle.Capacity);
                childWindows.Add((childHwnd, sbClass.ToString(), sbTitle.ToString()));
                return true;
            }, IntPtr.Zero);

            var element = AutomationElement.FromHandle(validTopHwnd);
            var editCondition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
            );
            var edits = element.FindAll(TreeScope.Descendants, editCondition);

            var targetEdit = childWindows.FirstOrDefault(c => c.ClassName.Equals("Edit", StringComparison.OrdinalIgnoreCase) || 
                                                              c.ClassName.Contains("RichEdit", StringComparison.OrdinalIgnoreCase));
            if (targetEdit.Hwnd != IntPtr.Zero)
            {
                _output.WriteLine($"Testing SendMessage WM_CHAR on child HWND 0x{targetEdit.Hwnd.ToInt64():X} ('{targetEdit.ClassName}')");

                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'H', (IntPtr)1);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'i', (IntPtr)1);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'z', (IntPtr)1);

                // Backspace
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_BACK, (IntPtr)0x000E0001);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)0x08, (IntPtr)0x000E0001);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_BACK, (IntPtr)unchecked((int)0xC00E0001));

                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'!', (IntPtr)1);

                // Enter
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_KEYDOWN, (IntPtr)Win32Api.VK_RETURN, (IntPtr)0x001C0001);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'\r', (IntPtr)0x001C0001);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_KEYUP, (IntPtr)Win32Api.VK_RETURN, (IntPtr)unchecked((int)0xC01C0001));

                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'L', (IntPtr)1);
                Win32Api.SendMessage(targetEdit.Hwnd, Win32Api.WM_CHAR, (IntPtr)'2', (IntPtr)1);

                if (edits.Count > 0 && edits[0].TryGetCurrentPattern(ValuePattern.Pattern, out var vpObj2))
                {
                    var vp = (ValuePattern)vpObj2;
                    _output.WriteLine($"Value after WM_CHAR typing, backspace, and newline: '{vp.Current.Value}'");
                    Assert.Contains("Hi!", vp.Current.Value);
                    Assert.Contains("L2", vp.Current.Value);
                    Assert.DoesNotContain("Hiz", vp.Current.Value);
                }
            }
        }
        finally
        {
            try
            {
                foreach (var np in Process.GetProcessesByName("notepad"))
                {
                    try { np.Kill(); } catch { }
                }
            }
            catch { }
        }
    }

    [Fact]
    public void TestAttachThreadInputGetFocusOnBackgroundWindow()
    {
        var proc = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
        Assert.NotNull(proc);

        try
        {
            Thread.Sleep(1500);

            IntPtr validTopHwnd = IntPtr.Zero;
            uint targetThreadId = 0;

            foreach (var np in Process.GetProcessesByName("notepad"))
            {
                Win32Api.EnumWindows((hWnd, lParam) =>
                {
                    uint tid = Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == np.Id && Win32Api.IsWindowVisible(hWnd))
                    {
                        var sbTitle = new StringBuilder(256);
                        Win32Api.GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                        if (sbTitle.Length > 0 && !sbTitle.ToString().Contains("InputIndicator"))
                        {
                            validTopHwnd = hWnd;
                            targetThreadId = tid;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            Assert.True(validTopHwnd != IntPtr.Zero);

            uint currentThreadId = Win32Api.GetCurrentThreadId();
            bool attached = Win32Api.AttachThreadInput(currentThreadId, targetThreadId, true);
            IntPtr focusHwnd = IntPtr.Zero;
            try
            {
                focusHwnd = Win32Api.GetFocus();
            }
            finally
            {
                if (attached) Win32Api.AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            _output.WriteLine($"Target Top HWND: 0x{validTopHwnd.ToInt64():X}, Thread ID: {targetThreadId}");
            _output.WriteLine($"Attached: {attached}, GetFocus HWND: 0x{focusHwnd.ToInt64():X}");

            if (focusHwnd != IntPtr.Zero)
            {
                var sbClass = new StringBuilder(256);
                Win32Api.GetClassName(focusHwnd, sbClass, sbClass.Capacity);
                _output.WriteLine($"Focused child ClassName: '{sbClass}'");
            }
        }
        finally
        {
            try
            {
                foreach (var np in Process.GetProcessesByName("notepad"))
                {
                    try { np.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}
