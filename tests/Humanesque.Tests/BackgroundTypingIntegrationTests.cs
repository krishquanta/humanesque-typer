using System.Diagnostics;
using Humanesque.Core;
using Humanesque.Models;
using Humanesque.Native;
using Xunit;

namespace Humanesque.Tests;

public class BackgroundTypingIntegrationTests
{
    [Fact]
    public async Task TypingEngine_TypesDirectlyIntoTargetWindow_InBackground()
    {
        // 1. Launch a background Notepad process
        var psi = new ProcessStartInfo
        {
            FileName = "notepad.exe",
            UseShellExecute = true
        };

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            IntPtr hWnd = IntPtr.Zero;
            for (int i = 0; i < 30; i++)
            {
                proc.Refresh();
                hWnd = proc.MainWindowHandle;
                if (hWnd != IntPtr.Zero) break;
                await Task.Delay(100);
            }

            if (hWnd == IntPtr.Zero)
            {
                // Fallback for Windows 11 tabbed Notepad where parent PID differs
                var npProcs = Process.GetProcessesByName("notepad");
                foreach (var np in npProcs)
                {
                    if (np.MainWindowHandle != IntPtr.Zero)
                    {
                        hWnd = np.MainWindowHandle;
                        break;
                    }
                }
            }

            Assert.NotEqual(IntPtr.Zero, hWnd);

            // 2. Resolve target edit control (Notepad Edit or RichEditD2DPT)
            var targetInfo = WindowTargetHelper.FromHandle(hWnd);
            Assert.NotNull(targetInfo);

            IntPtr targetEditHandle = targetInfo.EditControlHandle != IntPtr.Zero
                ? targetInfo.EditControlHandle
                : targetInfo.Handle;

            // 3. Configure a fast profile with 0% typos for determinism
            var fastProfile = new TypingProfile
            {
                Id = "deterministic_fast",
                Name = "Deterministic Fast",
                Description = "Fast test",
                TargetWpm = 120.0,
                WpmVariance = 2.0,
                TypoRatePercent = 0.0,
                MinOvertypeCount = 1,
                MaxOvertypeCount = 2,
                ReactionDelayMinMs = 50,
                ReactionDelayMaxMs = 100,
                BackspaceDelayMinMs = 20,
                BackspaceDelayMaxMs = 40,
                CommonWordBurstFactor = 0.5,
                CommaPauseMinMs = 20,
                CommaPauseMaxMs = 50,
                PeriodPauseMinMs = 40,
                PeriodPauseMaxMs = 80,
                ParagraphPauseMinMs = 50,
                ParagraphPauseMaxMs = 100
            };

            var engine = new TypingEngine();
            string testSentence = "Humanesque background typing test successful.";

            // 4. Run typing into the background Notepad window
            await engine.StartTypingAsync(testSentence, targetEditHandle, fastProfile);

            Assert.Equal(TypingEngineState.Completed, engine.State);
        }
        finally
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch { }
        }
    }
}
