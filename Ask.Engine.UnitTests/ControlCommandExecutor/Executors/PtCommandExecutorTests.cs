using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class PtCommandExecutorTests
{
  [Fact]
  public async Task ExecuteAsync_WithoutPoints_PerformsSpecifiedDelay()
  {
    var command = new PtCommandModel
    {
      CommandNumber = "10",
      SourceLines = ["10 ПТ 10мс"],
      TimeSource = "10мс",
      Time = 0.01,
      BusPointsDictionary = new()
    };
    var interaction = new Mock<IUserInteractionService>();
    interaction.Setup(service => service.GetCancellationToken()).Returns(CancellationToken.None);
    interaction.Setup(service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Returns(Task.CompletedTask);

    TimeSpan? actualDelay = null;
    interaction.As<IExecutionPauseGate>()
      .Setup(gate => gate.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
      .Callback<TimeSpan, CancellationToken>((delay, _) => actualDelay = delay)
      .Returns(Task.CompletedTask);

    var editor = new Mock<ITextEditorAdapter>();
    var manager = new CommandExecutionManager(interaction.Object, editor.Object, [command], null);
    var context = new CommandExecutionContext(manager, command, interaction.Object, editor.Object, string.Empty);

    await new PtCommandExecutor().ExecuteAsync(context, new ProtocolModel());

    Assert.Equal(TimeSpan.FromMilliseconds(10), actualDelay);
    interaction.As<IExecutionPauseGate>().Verify(
      gate => gate.DelayAsync(TimeSpan.FromMilliseconds(10), CancellationToken.None),
      Times.Once);
  }
}
