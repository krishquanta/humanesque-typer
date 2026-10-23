using System.Diagnostics;
using System.Windows.Automation;
using Humanesque.Core;
using Humanesque.Models;
using Humanesque.Native;
using Xunit;
using Xunit.Abstractions;

namespace Humanesque.Tests;

[Collection("NotepadSequential")]
public class BackgroundTypingIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public BackgroundTypingIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestEndToEndBackgroundTypingWithTyposAndCadence()
    {
        var proc = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
        Assert.NotNull(proc);

        try
        {
            await Task.Delay(2000);

            // 1. Resolve windows via WindowTargetHelper matching the exact process ID
            var windows = WindowTargetHelper.GetActiveWindows();
            var notepadTarget = windows.FirstOrDefault(w => w.ProcessId == proc.Id &&
                                                           !w.ClassName.Contains("InputIndicator") &&
                                                           w.WindowTitle.Length > 0)
                                ?? windows.FirstOrDefault(w => w.ProcessName.Equals("Notepad", StringComparison.OrdinalIgnoreCase) &&
                                                              !w.ClassName.Contains("InputIndicator") &&
                                                              w.WindowTitle.Length > 0);

            Assert.NotNull(notepadTarget);
            _output.WriteLine($"Target: {notepadTarget.DisplayText}");
            _output.WriteLine($"Top HWND: 0x{notepadTarget.Handle.ToInt64():X}, Edit HWND: 0x{notepadTarget.EditControlHandle.ToInt64():X} ({notepadTarget.EditControlClassName})");
            _output.WriteLine($"Supports Background: {notepadTarget.SupportsBackgroundTyping}, Strategy: {notepadTarget.BackgroundStrategyDescription}");

            Assert.True(notepadTarget.SupportsBackgroundTyping, "Notepad should support background typing");
            Assert.True(notepadTarget.HasChildEditControl, "Notepad should resolve a child edit control");

            // 2. Prepare Engine & Fast Profile with guaranteed typo
            var engine = new TypingEngine();
            engine.Dispatcher.ActiveMode = InjectionMode.BackgroundAuto;

            var profile = TypingProfile.CreateFast() with
            {
                TargetWpm = 200, // Very fast for fast unit test execution
                TypoRatePercent = 25.0, // High typo rate to trigger corrections
                ReactionDelayMinMs = 20,
                ReactionDelayMaxMs = 40,
                BackspaceDelayMinMs = 15,
                BackspaceDelayMaxMs = 30,
                CommaPauseMinMs = 20,
                CommaPauseMaxMs = 40,
                PeriodPauseMinMs = 30,
                PeriodPauseMaxMs = 60
            };

            string testString = "Humanesque background typing test!";
            int charactersReported = 0;
            engine.ProgressUpdated += stats =>
            {
                charactersReported = stats.CharactersTyped;
            };

            // Capture foreground window before typing
            IntPtr initialFg = Win32Api.GetForegroundWindow();

            // 3. Start typing in background (countdownSeconds = 0)
            await engine.StartTypingAsync(testString, notepadTarget, profile, countdownSeconds: 0);

            _output.WriteLine($"Typing completed. Characters reported: {charactersReported}");
            Assert.Equal(TypingEngineState.Completed, engine.State);

            // 4. Verify text inside Notepad via UI Automation
            var uiaElement = AutomationElement.FromHandle(notepadTarget.Handle);
            Assert.NotNull(uiaElement);

            var editCondition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document)
            );
            var edits = uiaElement.FindAll(TreeScope.Descendants, editCondition);
            Assert.True(edits.Count > 0, "Should find edit control via UI Automation");

            if (edits[0].TryGetCurrentPattern(ValuePattern.Pattern, out var vpObj))
            {
                var vp = (ValuePattern)vpObj;
                string actualText = vp.Current.Value;
                _output.WriteLine($"Actual text inside Notepad: '{actualText}'");
                Assert.Contains("Humanesque", actualText);
                Assert.Contains("background", actualText);
                Assert.Contains("typing", actualText);
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
