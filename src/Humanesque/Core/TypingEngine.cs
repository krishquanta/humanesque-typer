using System.Diagnostics;
using Humanesque.Models;
using Humanesque.Native;

namespace Humanesque.Core;

public enum TypingEngineState
{
    Idle,
    CountingDown,
    Running,
    Paused,
    Completed,
    Aborted
}

public class TypingEngine
{
    private readonly SpatialTypoEngine _typoEngine = new();
    private readonly BiomimeticCadence _cadence = new();
    private readonly OvertypeCorrectionStateMachine _stateMachine = new();
    private readonly InputDispatcher _dispatcher = new();

    private CancellationTokenSource? _cts;
    private ManualResetEventSlim _pauseEvent = new(true);
    private Stopwatch _sessionStopwatch = new();

    public TypingEngineState State { get; private set; } = TypingEngineState.Idle;
    public InputDispatcher Dispatcher => _dispatcher;

    public event Action<SessionStats>? ProgressUpdated;
    public event Action<TypingEngineState>? StateChanged;
    public event Action<int>? CountdownTick;

    public void Pause()
    {
        if (State == TypingEngineState.Running)
        {
            _pauseEvent.Reset();
            _sessionStopwatch.Stop();
            State = TypingEngineState.Paused;
            StateChanged?.Invoke(State);
        }
    }

    public void Resume()
    {
        if (State == TypingEngineState.Paused)
        {
            _pauseEvent.Set();
            _sessionStopwatch.Start();
            State = TypingEngineState.Running;
            StateChanged?.Invoke(State);
        }
    }

    public void Abort()
    {
        if (State == TypingEngineState.Running || State == TypingEngineState.Paused || State == TypingEngineState.CountingDown)
        {
            _cts?.Cancel();
            _pauseEvent.Set();
            _sessionStopwatch.Stop();
            State = TypingEngineState.Aborted;
            StateChanged?.Invoke(State);
        }
    }

    public async Task StartTypingAsync(string text, TargetWindowInfo target, TypingProfile profile, int countdownSeconds = 3)
    {
        if (string.IsNullOrEmpty(text) || target.Handle == IntPtr.Zero) return;

        _cts = new CancellationTokenSource();
        _pauseEvent.Set();
        var token = _cts.Token;

        // Initialize and prepare injection channel
        _dispatcher.PrepareTarget(target);

        // If using FocusSendInput mode or auto-detecting a non-Word window, give a clean countdown and focus window
        bool requiresFocus = _dispatcher.ActiveMode == InjectionMode.FocusSendInput ||
                             (_dispatcher.ActiveMode == InjectionMode.AutoDetect && !target.ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase));

        if (requiresFocus && countdownSeconds > 0)
        {
            State = TypingEngineState.CountingDown;
            StateChanged?.Invoke(State);

            for (int s = countdownSeconds; s > 0; s--)
            {
                token.ThrowIfCancellationRequested();
                CountdownTick?.Invoke(s);
                await Task.Delay(1000, token);
            }

            // Bring target window to foreground
            InputDispatcher.ActivateWindow(target.Handle);
            await Task.Delay(250, token);
        }

        _sessionStopwatch.Restart();
        State = TypingEngineState.Running;
        StateChanged?.Invoke(State);

        var stats = new SessionStats
        {
            TotalCharacters = text.Length,
            CharactersTyped = 0,
            TyposIntroduced = 0,
            BackspacesDispatched = 0
        };

        using var timer = new HighResolutionTimer();

        await Task.Run(() =>
        {
            int index = 0;

            try
            {
                while (index < text.Length)
                {
                    token.ThrowIfCancellationRequested();
                    _pauseEvent.Wait(token);

                    char intendedChar = text[index];

                    // Check if a human typo should be triggered at this point
                    if (_typoEngine.ShouldTriggerTypo(intendedChar, profile) && (index + 2 < text.Length))
                    {
                        stats.TyposIntroduced++;
                        _stateMachine.StartTypo(intendedChar);

                        // 1. Dispatch the erroneous spatial neighbor
                        char typoChar = _typoEngine.GenerateTypoCharacter(intendedChar);
                        _dispatcher.DispatchChar(target, typoChar);

                        int delay = _cadence.CalculateKeyInterval(typoChar, text, index, profile);
                        HighResolutionTimer.Delay(delay, token);

                        // 2. Overtype lag: type 1 to 3 additional letters before noticing
                        int overtypeCount = _typoEngine.GetOvertypeCount(profile);
                        int overtyped = 0;

                        while (overtyped < overtypeCount && (index + 1 + overtyped < text.Length))
                        {
                            token.ThrowIfCancellationRequested();
                            _pauseEvent.Wait(token);

                            char nextChar = text[index + 1 + overtyped];
                            _stateMachine.RecordOvertype(nextChar);
                            _dispatcher.DispatchChar(target, nextChar);

                            int overDelay = _cadence.CalculateKeyInterval(nextChar, text, index + 1 + overtyped, profile);
                            HighResolutionTimer.Delay(overDelay, token);

                            overtyped++;
                        }

                        // 3. Cognitive hesitation pause (human brain realizes the mistake)
                        int reactionPause = _cadence.GetReactionHesitation(profile);
                        HighResolutionTimer.Delay(reactionPause, token);

                        // 4. Backspace sequence
                        int backspaces = _stateMachine.GetBackspaceCount();
                        for (int b = 0; b < backspaces; b++)
                        {
                            token.ThrowIfCancellationRequested();
                            _pauseEvent.Wait(token);

                            _dispatcher.DispatchBackspace(target);
                            stats.BackspacesDispatched++;

                            int backspaceDelay = _cadence.GetBackspaceInterval(profile);
                            HighResolutionTimer.Delay(backspaceDelay, token);
                        }

                        // Brief micro-pause before re-typing
                        HighResolutionTimer.Delay(100, token);

                        // 5. Re-type the sequence correctly
                        var replayChars = _stateMachine.GetReplaySequence();
                        for (int r = 0; r < replayChars.Count; r++)
                        {
                            token.ThrowIfCancellationRequested();
                            _pauseEvent.Wait(token);

                            char replayChar = replayChars[r];
                            _dispatcher.DispatchChar(target, replayChar);

                            stats.CharactersTyped++;
                            UpdateTelemetry(stats);

                            int replayDelay = _cadence.CalculateKeyInterval(replayChar, text, index + r, profile);
                            HighResolutionTimer.Delay(replayDelay, token);
                        }

                        index += replayChars.Count;
                        _stateMachine.Reset();
                    }
                    else
                    {
                        // Standard keystroke dispatch
                        _dispatcher.DispatchChar(target, intendedChar);
                        stats.CharactersTyped++;
                        index++;

                        UpdateTelemetry(stats);

                        int delay = _cadence.CalculateKeyInterval(intendedChar, text, index - 1, profile);
                        HighResolutionTimer.Delay(delay, token);
                    }
                }

                _sessionStopwatch.Stop();
                State = TypingEngineState.Completed;
                StateChanged?.Invoke(State);
            }
            catch (OperationCanceledException)
            {
                _sessionStopwatch.Stop();
                State = TypingEngineState.Aborted;
                StateChanged?.Invoke(State);
            }
            finally
            {
                _sessionStopwatch.Stop();
            }
        }, token);
    }

    private void UpdateTelemetry(SessionStats stats)
    {
        double elapsedMinutes = _sessionStopwatch.Elapsed.TotalMinutes;
        stats.ElapsedTime = _sessionStopwatch.Elapsed;

        if (elapsedMinutes > 0.005)
        {
            stats.CurrentWpm = Math.Round((stats.CharactersTyped / 5.0) / elapsedMinutes, 1);
            stats.AverageWpm = stats.CurrentWpm;

            if (stats.CurrentWpm > 0 && stats.TotalCharacters > stats.CharactersTyped)
            {
                int remainingChars = stats.TotalCharacters - stats.CharactersTyped;
                double remainingMinutes = (remainingChars / 5.0) / stats.CurrentWpm;
                stats.EstimatedTimeRemaining = TimeSpan.FromMinutes(remainingMinutes);
            }
        }

        ProgressUpdated?.Invoke(stats);
    }
}
