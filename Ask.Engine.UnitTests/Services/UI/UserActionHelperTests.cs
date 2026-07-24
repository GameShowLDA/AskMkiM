using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Moq;

namespace Ask.Engine.UnitTests.Services.UI;

public sealed class UserActionHelperTests
{
  [Fact]
  public async Task SuccessfulInitialAttemptDoesNotRequestUserAction()
  {
    var interaction = CreateInteractionService();
    int calls = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () =>
      {
        calls++;
        return Task.FromResult(true);
      },
      interaction.Object,
      deviceTask: true);

    Assert.True(result);
    Assert.Equal(1, calls);
    interaction.Verify(
      service => service.WaitUserActionAsync(
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>()),
      Times.Never);
  }

  [Fact]
  public async Task HardwareFailureIgnoresStopSettingAndEnablesContinueOnlyAfterSuccess()
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(
      requests,
      UserAction.Retry,
      UserAction.Continue);
    var results = new Queue<bool>([false, true]);
    int calls = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () =>
      {
        calls++;
        return Task.FromResult(results.Dequeue());
      },
      interaction.Object,
      deviceTask: true);

    Assert.True(result);
    Assert.Equal(2, calls);
    Assert.Equal(
      [
        (false, true, false),
        (true, true, true),
      ],
      requests);
  }

  [Fact]
  public async Task SuccessfulRetryIsNotExecutedAgainWhenOperatorContinues()
  {
    var interaction = CreateInteractionService(
      requests: null,
      UserAction.Retry,
      UserAction.Continue);
    var buttons = new Mock<IButtonService>();
    interaction
      .SetupGet(service => service.ButtonService)
      .Returns(buttons.Object);
    int calls = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () => Task.FromResult(++calls > 1),
      interaction.Object,
      deviceTask: true);

    Assert.True(result);
    Assert.Equal(2, calls);
    buttons.Verify(
      service => service.ShowOnlyStopAndFinishButtons(),
      Times.Once);
  }

  [Fact]
  public async Task HardwareStateFollowsResultOfEveryRetry()
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(
      requests,
      UserAction.Retry,
      UserAction.Retry,
      UserAction.Retry,
      UserAction.Continue);
    var results = new Queue<bool>([false, true, false, true]);

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () => Task.FromResult(results.Dequeue()),
      interaction.Object,
      deviceTask: true);

    Assert.True(result);
    Assert.Equal(
      [false, true, false, true],
      requests.Select(request => request.CanContinue));
  }

  [Fact]
  public async Task MeasurementFailureCanContinueWithoutRetry()
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(requests, UserAction.Continue);
    int calls = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () =>
      {
        calls++;
        return Task.FromResult(false);
      },
      interaction.Object);

    Assert.False(result);
    Assert.Equal(1, calls);
    Assert.Equal([(false, false, true)], requests);
  }

  [Fact]
  public async Task MeasurementFailureReturnsImmediatelyWhenInteractionIsDisabled()
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(requests, UserAction.None);
    int calls = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () =>
      {
        calls++;
        return Task.FromResult(false);
      },
      interaction.Object);

    Assert.False(result);
    Assert.Equal(1, calls);
    Assert.Equal([(false, false, true)], requests);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task MeasurementRetryRemainsInteractiveForAnyValidResult(bool retryResult)
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(
      requests,
      UserAction.Retry,
      UserAction.Continue);
    var results = new Queue<bool>([false, retryResult]);

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () => Task.FromResult(results.Dequeue()),
      interaction.Object);

    Assert.Equal(retryResult, result);
    Assert.Equal([true, true], requests.Select(request => request.CanContinue));
  }

  [Fact]
  public async Task MeasurementRetrySwitchesToHardwareErrorAndBackToValidMeasurement()
  {
    var requests = new List<(bool Force, bool Hardware, bool CanContinue)>();
    var interaction = CreateInteractionService(
      requests,
      UserAction.Retry,
      UserAction.Retry,
      UserAction.Continue);
    int attempt = 0;

    bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () =>
      {
        attempt++;
        return attempt switch
        {
          1 => Task.FromResult(false),
          2 => Task.FromException<bool>(new InvalidOperationException("timeout")),
          _ => Task.FromResult(false),
        };
      },
      interaction.Object);

    Assert.False(result);
    Assert.Equal(
      [true, false, true],
      requests.Select(request => request.CanContinue));
    Assert.Equal(
      [false, true, false],
      requests.Select(request => request.Hardware));
  }

  [Fact]
  public async Task TypedSuccessfulRetryReturnsValueFromSuccessfulAttempt()
  {
    var interaction = CreateInteractionService(
      requests: null,
      UserAction.Retry,
      UserAction.Continue);
    var results = new Queue<OperationResult>(
    [
      new OperationResult(false, 0),
      new OperationResult(true, 42),
    ]);

    OperationResult result = await UserActionHelper.GetRunWithUserRepeatAsync(
      () => Task.FromResult(results.Dequeue()),
      value => value.Success,
      interaction.Object,
      deviceTask: true);

    Assert.True(result.Success);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public async Task MandatoryFinalizationDoesNotRequestUserAction()
  {
    var interaction = CreateInteractionService();
    int calls = 0;

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () =>
        {
          calls++;
          return Task.FromResult(false);
        },
        interaction.Object,
        deviceTask: true);

      Assert.False(result);
    }

    Assert.Equal(1, calls);
    interaction.Verify(
      service => service.WaitUserActionAsync(
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>()),
      Times.Never);
  }

  [Fact]
  public async Task FinishCancelsCurrentCallChain()
  {
    var interaction = CreateInteractionService(
      requests: null,
      UserAction.Abort);

    await Assert.ThrowsAsync<OperationCanceledException>(
      () => UserActionHelper.GetRunWithUserRepeatAsync(
        () => Task.FromResult(false),
        interaction.Object,
        deviceTask: true));
  }

  private static Mock<IUserInteractionService> CreateInteractionService(
    List<(bool Force, bool Hardware, bool CanContinue)>? requests = null,
    params UserAction[] actions)
  {
    var actionQueue = new Queue<UserAction>(actions);
    var interaction = new Mock<IUserInteractionService>();
    interaction
      .Setup(service => service.GetCancellationToken())
      .Returns(CancellationToken.None);
    interaction
      .Setup(service => service.WaitUserActionAsync(
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>()))
      .ReturnsAsync((bool force, bool hardware, bool canContinue) =>
      {
        requests?.Add((force, hardware, canContinue));
        return actionQueue.Count == 0 ? UserAction.None : actionQueue.Dequeue();
      });

    return interaction;
  }

  private sealed record OperationResult(bool Success, int Value);
}
