using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Exceptions;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.UnitTests.TestInfrastructure;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor;

public sealed class CommandExecutionManagerSkipCurrentCommandTests
{
  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public Task RequestNextCommandAsync_SkipsCurrentAndContinues(bool initiallyPaused) =>
    WpfTestHost.RunAsync(async () =>
    {
      var pauseReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var pauseInterrupted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var transitionPaused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var continueExecution = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      bool isPaused = initiallyPaused;
      int checkpointCount = 0;

      var console = new Mock<IUserInteractionService>();
      console.Setup(x => x.GetCancellationToken()).Returns(CancellationToken.None);
      console.Setup(x => x.ShowMessageAsync(
          It.IsAny<ShowMessageModel>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
          It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
        .Returns(Task.CompletedTask);
      console.Setup(x => x.CompleteCommandAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);

      console.As<IExecutionCommandJumpGate>()
        .SetupGet(x => x.IsExecutionPaused)
        .Returns(() => isPaused);
      console.As<IExecutionCommandJumpGate>()
        .Setup(x => x.PauseBeforeNextCommandAsync())
        .Returns(async () =>
        {
          transitionPaused.TrySetResult();
          await continueExecution.Task;
        });
      console.As<IExecutionCommandJumpGate>()
        .Setup(x => x.InterruptCurrentCommand())
        .Callback(() =>
        {
          isPaused = false;
          pauseInterrupted.TrySetResult();
        });
      console.As<IExecutionPauseGate>()
        .Setup(x => x.WaitIfPausedAsync(It.IsAny<CancellationToken>()))
        .Returns(async () =>
        {
          if (Interlocked.Increment(ref checkpointCount) != 3)
          {
            return;
          }

          pauseReached.TrySetResult();
          await pauseInterrupted.Task;
          throw new CommandJumpRequestedException();
        });

      var editor = new Mock<ITextEditorAdapter>();
      var current = new CpCommandModel
      {
        CommandNumber = "10",
        SourceLines = ["10 СП"],
        FormattedStartLineNumber = 10,
      };
      var next = new CpCommandModel
      {
        CommandNumber = "20",
        SourceLines = ["20 СП"],
        FormattedStartLineNumber = 20,
      };
      var manager = new CommandExecutionManager(console.Object, editor.Object, [current, next], null);

      var execution = manager.ExecuteAllAsync();
      await pauseReached.Task.WaitAsync(TimeSpan.FromSeconds(3));
      await CommandExecutionManager.RequestNextCommandAsync();
      await transitionPaused.Task.WaitAsync(TimeSpan.FromSeconds(3));
      Assert.False(execution.IsCompleted);
      console.Verify(x => x.CompleteCommandAsync(It.IsAny<bool>()), Times.Never);
      console.As<IExecutionCommandJumpGate>().Verify(x => x.PauseBeforeNextCommandAsync(), Times.Once);
      continueExecution.TrySetResult();
      await execution.WaitAsync(TimeSpan.FromSeconds(3));

      editor.Verify(x => x.SetActiveLine(10), Times.AtLeastOnce);
      editor.Verify(x => x.SetActiveLine(20), Times.AtLeastOnce);
      console.Verify(x => x.CompleteCommandAsync(false), Times.Once);
    });
}
