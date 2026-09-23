using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandExecutor.Executors;
using Moq;
using System.Windows;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class CuCommandExecutorTests
{
  [Fact]
  public async Task ProcessQuestionResultAsync_NoWithConditionalJump_RequestsJumpWithoutStop()
  {
    var interactionService = new Mock<IUserInteractionService>();

    var shouldJump = await CuCommandExecutor.ProcessQuestionResultAsync(
      interactionService.Object,
      MessageBoxResult.No,
      hasConditionalJump: true);

    Assert.True(shouldJump);
    interactionService.Verify(
      service => service.WaitUserActionAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()),
      Times.Never);
  }

  [Fact]
  public async Task ProcessQuestionResultAsync_NoWithoutConditionalJump_WaitsForResume()
  {
    var interactionService = CreateInteractionService(UserAction.Continue);

    var shouldJump = await CuCommandExecutor.ProcessQuestionResultAsync(
      interactionService.Object,
      MessageBoxResult.No,
      hasConditionalJump: false);

    Assert.False(shouldJump);
    interactionService.Verify(
      service => service.WaitUserActionAsync(false, true, true),
      Times.Once);
  }

  [Fact]
  public async Task ProcessQuestionResultAsync_Cancel_WaitsForResume()
  {
    var interactionService = CreateInteractionService(UserAction.Continue);

    var shouldJump = await CuCommandExecutor.ProcessQuestionResultAsync(
      interactionService.Object,
      MessageBoxResult.Cancel,
      hasConditionalJump: true);

    Assert.False(shouldJump);
    interactionService.Verify(
      service => service.WaitUserActionAsync(false, true, true),
      Times.Once);
  }

  [Fact]
  public async Task ProcessQuestionResultAsync_StopAction_ThrowsOperationCanceledException()
  {
    var interactionService = CreateInteractionService(UserAction.Abort);

    await Assert.ThrowsAsync<OperationCanceledException>(() =>
      CuCommandExecutor.ProcessQuestionResultAsync(
        interactionService.Object,
        MessageBoxResult.No,
        hasConditionalJump: false));
  }

  private static Mock<IUserInteractionService> CreateInteractionService(UserAction action)
  {
    var interactionService = new Mock<IUserInteractionService>();
    interactionService
      .Setup(service => service.WaitUserActionAsync(false, true, true))
      .ReturnsAsync(action);
    return interactionService;
  }
}
