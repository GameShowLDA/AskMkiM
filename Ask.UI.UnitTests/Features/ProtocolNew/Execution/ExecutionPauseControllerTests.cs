using Ask.UI.Features.ProtocolNew.Execution;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionPauseControllerTests
{
  [Fact(DisplayName = "Ожидание во время выполнения завершается сразу")]
  public async Task WaitAsync_WhenExecutionIsRunning_CompletesImmediately()
  {
    var controller = new ExecutionPauseController();

    await controller.WaitAsync(CancellationToken.None);

    Assert.False(controller.IsPaused);
  }

  [Fact(DisplayName = "Повторный запрос паузы отклоняется")]
  public void RequestPause_WhenPauseIsAlreadyRequested_ReturnsFalse()
  {
    var controller = new ExecutionPauseController();

    var firstRequestAccepted = controller.RequestPause();
    var secondRequestAccepted = controller.RequestPause();

    Assert.True(firstRequestAccepted);
    Assert.False(secondRequestAccepted);
    Assert.True(controller.IsPaused);
  }

  [Fact(DisplayName = "Запрос паузы завершает ожидание сигнала")]
  public async Task WaitForPauseRequestAsync_WhenPauseIsRequested_Completes()
  {
    var controller = new ExecutionPauseController();
    var pauseRequestTask = controller.WaitForPauseRequestAsync(CancellationToken.None);

    await Task.Yield();
    Assert.False(pauseRequestTask.IsCompleted);

    Assert.True(controller.RequestPause());
    await pauseRequestTask.WaitAsync(TimeSpan.FromSeconds(1));

    Assert.True(controller.IsPaused);
  }

  [Fact(DisplayName = "Продолжение освобождает все приостановленные операции")]
  public async Task Resume_WhenSeveralOperationsArePaused_ReleasesEveryOperation()
  {
    var controller = new ExecutionPauseController();
    Assert.True(controller.RequestPause());
    var waitTasks = Enumerable.Range(0, 16)
      .Select(_ => controller.WaitAsync(CancellationToken.None))
      .ToArray();

    await Task.Yield();
    Assert.All(waitTasks, task => Assert.False(task.IsCompleted));

    controller.Resume();
    await Task.WhenAll(waitTasks).WaitAsync(TimeSpan.FromSeconds(1));

    Assert.False(controller.IsPaused);
  }

  [Fact(DisplayName = "Завершение отменяет ожидание паузы и сбрасывает состояние")]
  public async Task Cancel_WhenOperationIsPaused_CancelsWaitingAndClearsState()
  {
    var controller = new ExecutionPauseController();
    Assert.True(controller.RequestPause());
    var waitTask = controller.WaitAsync(CancellationToken.None);

    controller.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
    Assert.False(controller.IsPaused);
    await controller.WaitAsync(CancellationToken.None);
  }

  [Fact(DisplayName = "Сброс освобождает ожидающую операцию и очищает состояние")]
  public async Task Reset_WhenOperationIsPaused_ReleasesWaitingAndClearsState()
  {
    var controller = new ExecutionPauseController();
    Assert.True(controller.RequestPause());
    var waitTask = controller.WaitAsync(CancellationToken.None);

    controller.Reset();
    await waitTask.WaitAsync(TimeSpan.FromSeconds(1));

    Assert.False(controller.IsPaused);
  }
}
