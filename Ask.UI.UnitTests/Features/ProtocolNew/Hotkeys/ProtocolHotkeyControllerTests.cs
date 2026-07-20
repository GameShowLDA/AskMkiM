using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Hotkeys;

public sealed class ProtocolHotkeyControllerTests
{
  [Fact(DisplayName = "F5 вызывает запуск, паузу или продолжение")]
  public Task F5_WhenPressed_InvokesRunOrPause() => RunOnStaAsync(() =>
  {
    var context = new RecordingHotkeyContext { CanPause = true };
    var args = CreateKeyEventArgs(Key.F5);

    new ProtocolHotkeyController(context).HandleKeyDown(this, args);

    Assert.Equal(1, context.RunOrPauseCount);
    Assert.True(args.Handled);
  });

  [Fact(DisplayName = "P при доступной паузе приостанавливает выполнение")]
  public Task P_WhenPauseIsAvailable_InvokesPause() => RunOnStaAsync(() =>
  {
    var context = new RecordingHotkeyContext { CanPause = true };
    var args = CreateKeyEventArgs(Key.P);

    new ProtocolHotkeyController(context).HandleKeyDown(this, args);

    Assert.Equal(1, context.PauseCount);
    Assert.Equal(0, context.ContinueCount);
    Assert.True(args.Handled);
  });

  [Fact(DisplayName = "P при доступном продолжении возобновляет выполнение")]
  public Task P_WhenContinueIsAvailable_InvokesContinue() => RunOnStaAsync(() =>
  {
    var context = new RecordingHotkeyContext { CanContinue = true };
    var args = CreateKeyEventArgs(Key.P);

    new ProtocolHotkeyController(context).HandleKeyDown(this, args);

    Assert.Equal(1, context.ContinueCount);
    Assert.Equal(0, context.PauseCount);
    Assert.True(args.Handled);
  });

  [Theory(DisplayName = "Клавиша пошагового режима запускает требуемый тип шага")]
  [InlineData(Key.F10, false)]
  [InlineData(Key.F11, true)]
  public Task StepKey_WhenPressed_InvokesExpectedStep(Key key, bool isStepInto) => RunOnStaAsync(() =>
  {
    var context = new RecordingHotkeyContext();
    var args = CreateKeyEventArgs(key);

    new ProtocolHotkeyController(context).HandleKeyDown(this, args);

    Assert.Equal([isStepInto], context.StepModes);
    Assert.True(args.Handled);
  });

  [Fact(DisplayName = "Escape при доступном завершении останавливает выполнение")]
  public Task Escape_WhenExitIsAvailable_InvokesExit() => RunOnStaAsync(() =>
  {
    var context = new RecordingHotkeyContext { CanExit = true };
    var args = CreateKeyEventArgs(Key.Escape);

    new ProtocolHotkeyController(context).HandleKeyDown(this, args);

    Assert.Equal(1, context.ExitCount);
    Assert.True(args.Handled);
  });

  private static KeyEventArgs CreateKeyEventArgs(Key key) =>
    new(InputManager.Current.PrimaryKeyboardDevice, new TestPresentationSource(), 0, key)
    {
      RoutedEvent = Keyboard.KeyDownEvent
    };

  private static async Task RunOnStaAsync(Action action)
  {
    var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var thread = new Thread(() =>
    {
      try
      {
        action();
        completion.TrySetResult(true);
      }
      catch (Exception exception)
      {
        completion.TrySetException(exception);
      }
    })
    {
      IsBackground = true
    };

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
  }

  private sealed class RecordingHotkeyContext : IProtocolHotkeyContext
  {
    public bool CanStart { get; init; }

    public bool CanPause { get; init; }

    public bool CanContinue { get; init; }

    public bool CanExit { get; init; }

    public bool CanRepeat { get; init; }

    public int RunOrPauseCount { get; private set; }

    public int PauseCount { get; private set; }

    public int ContinueCount { get; private set; }

    public int ExitCount { get; private set; }

    public List<bool> StepModes { get; } = [];

    public void Start()
    {
    }

    public void RunOrPause() => RunOrPauseCount++;

    public void Step(bool isStepInto) => StepModes.Add(isStepInto);

    public void Pause() => PauseCount++;

    public void Continue() => ContinueCount++;

    public void Exit() => ExitCount++;

    public void Repeat()
    {
    }

    public void NotifyOtherKey(object sender, KeyEventArgs e)
    {
    }
  }

  private sealed class TestPresentationSource : PresentationSource
  {
    public override Visual RootVisual { get; set; } = new DrawingVisual();

    public override bool IsDisposed => false;

    protected override CompositionTarget GetCompositionTargetCore() => null!;
  }
}
