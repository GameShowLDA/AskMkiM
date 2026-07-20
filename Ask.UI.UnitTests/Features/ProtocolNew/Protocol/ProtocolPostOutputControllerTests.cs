using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.ProtocolNew.Protocol;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Protocol;

public sealed class ProtocolPostOutputControllerTests
{
  [Fact(DisplayName = "Служебное сообщение не ожидает снятия паузы")]
  public async Task ProcessAsync_WhenPauseIsSkipped_DoesNotWaitForResume()
  {
    var context = new RecordingPostOutputContext { IsPaused = true };
    var controller = new ProtocolPostOutputController(context);

    await controller.ProcessAsync(
      new ShowMessageModel(),
      isBlockStart: false,
      skipStepModeCheck: true,
      skipPause: true);

    Assert.Equal(0, context.PauseWaitCount);
  }

  [Fact(DisplayName = "Обычное сообщение ожидает снятия паузы")]
  public async Task ProcessAsync_WhenPaused_WaitsForResume()
  {
    var context = new RecordingPostOutputContext { IsPaused = true };
    var controller = new ProtocolPostOutputController(context);

    await controller.ProcessAsync(
      new ShowMessageModel(),
      isBlockStart: false,
      skipStepModeCheck: true,
      skipPause: false);

    Assert.Equal(1, context.PauseWaitCount);
  }

  private sealed class RecordingPostOutputContext : IProtocolPostOutputContext
  {
    public bool IsPaused { get; init; }

    public int PauseWaitCount { get; private set; }

    public CancellationToken GetCancellationToken() => CancellationToken.None;

    public Task WaitWhilePausedAsync(CancellationToken cancellationToken)
    {
      PauseWaitCount++;
      return Task.CompletedTask;
    }

    public Task PauseAsync() => Task.CompletedTask;

    public void ShowPauseButtons()
    {
    }

    public void ShowRunningButtons(bool showStepButtons)
    {
    }
  }
}
