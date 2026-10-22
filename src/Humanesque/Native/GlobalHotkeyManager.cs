using System.Windows.Interop;

namespace Humanesque.Native;

public class GlobalHotkeyManager : IDisposable
{
    private const int HOTKEY_ID_PAUSE_RESUME = 9001;
    private const int HOTKEY_ID_ABORT = 9002;
    private const int HOTKEY_ID_LOCK_TARGET = 9003;

    private HwndSource? _hwndSource;
    private IntPtr _windowHandle;

    public event Action? PauseResumeRequested;
    public event Action? AbortRequested;
    public event Action? LockTargetRequested;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(HwndHook);

        RegisterDefaultHotkeys();
    }

    private void RegisterDefaultHotkeys()
    {
        // F8: Pause / Resume
        Win32Api.RegisterHotKey(_windowHandle, HOTKEY_ID_PAUSE_RESUME, Win32Api.MOD_NOREPEAT, 0x77); // VK_F8

        // F9: Emergency Abort
        Win32Api.RegisterHotKey(_windowHandle, HOTKEY_ID_ABORT, Win32Api.MOD_NOREPEAT, 0x78); // VK_F9

        // Ctrl + Shift + T: Lock Current Target Window
        Win32Api.RegisterHotKey(_windowHandle, HOTKEY_ID_LOCK_TARGET, Win32Api.MOD_CONTROL | Win32Api.MOD_SHIFT | Win32Api.MOD_NOREPEAT, 0x54); // 'T'
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Api.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_ID_PAUSE_RESUME:
                    PauseResumeRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_ABORT:
                    AbortRequested?.Invoke();
                    handled = true;
                    break;
                case HOTKEY_ID_LOCK_TARGET:
                    LockTargetRequested?.Invoke();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            Win32Api.UnregisterHotKey(_windowHandle, HOTKEY_ID_PAUSE_RESUME);
            Win32Api.UnregisterHotKey(_windowHandle, HOTKEY_ID_ABORT);
            Win32Api.UnregisterHotKey(_windowHandle, HOTKEY_ID_LOCK_TARGET);
        }
        _hwndSource?.RemoveHook(HwndHook);
    }
}
