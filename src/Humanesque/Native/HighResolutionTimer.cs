using System.Diagnostics;

namespace Humanesque.Native;

/// <summary>
/// Provides millisecond-precise timing for human typing intervals by enabling
/// Windows multimedia timer period resolution (timeBeginPeriod(1)).
/// </summary>
public sealed class HighResolutionTimer : IDisposable
{
    private bool _isPeriodActive;

    public HighResolutionTimer()
    {
        // Request 1ms timer resolution from Windows multimedia subsystem
        if (Win32Api.TimeBeginPeriod(1) == 0)
        {
            _isPeriodActive = true;
        }
    }

    /// <summary>
    /// Suspends the current thread for the specified millisecond duration with high precision.
    /// Uses Thread.Sleep for the bulk delay and a micro spin-wait for the tail millisecond to prevent overshoot.
    /// </summary>
    public static void Delay(int milliseconds, CancellationToken cancellationToken)
    {
        if (milliseconds <= 0) return;

        var sw = Stopwatch.StartNew();

        if (milliseconds > 2)
        {
            // Sleep the majority of the time to keep CPU consumption at 0%
            int sleepTarget = milliseconds - 2;
            while (sw.ElapsedMilliseconds < sleepTarget)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }
        }

        // Spin-wait the remaining fraction to hit exact millisecond target
        while (sw.ElapsedMilliseconds < milliseconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Thread.SpinWait(10);
        }
    }

    public void Dispose()
    {
        if (_isPeriodActive)
        {
            Win32Api.TimeEndPeriod(1);
            _isPeriodActive = false;
        }
    }
}
