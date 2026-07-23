using Ask.Core.Services.UI;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Features.ProtocolNew.Execution;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionFinalizerTests
{
  [Theory]
  [InlineData(CheckType.ControlProgram, false)]
  [InlineData(CheckType.SelfTest, true)]
  [InlineData(CheckType.Test, true)]
  [InlineData(CheckType.Metrology, true)]
  public void CompletionHeaderIsHiddenOnlyForControlProgram(
    CheckType checkType,
    bool expected)
  {
    Assert.Equal(expected, ActionExecutor.ShouldShowTestCompletionHeader(checkType));
  }

  [Fact]
  public async Task MandatoryStepsContinueAfterFailureAndSuppressInteractiveContext()
  {
    var completedSteps = new List<string>();
    var contextStates = new List<bool>();

    await ExecutionFinalizer.RunMandatoryStepsAsync(
      ("ошибка", () =>
      {
        contextStates.Add(EquipmentExecutionContext.IsMandatoryFinalization);
        return Task.FromException(new InvalidOperationException("reset failed"));
      }),
      ("следующий шаг", () =>
      {
        contextStates.Add(EquipmentExecutionContext.IsMandatoryFinalization);
        completedSteps.Add("следующий шаг");
        return Task.CompletedTask;
      }),
      ("последний шаг", () =>
      {
        contextStates.Add(EquipmentExecutionContext.IsMandatoryFinalization);
        completedSteps.Add("последний шаг");
        return Task.CompletedTask;
      }));

    Assert.Equal(["следующий шаг", "последний шаг"], completedSteps);
    Assert.All(contextStates, Assert.True);
    Assert.False(EquipmentExecutionContext.IsMandatoryFinalization);
  }

  [Theory]
  [InlineData("SUCCESS", false)]
  [InlineData("FAILURE", false)]
  [InlineData("USER STOP", true)]
  [InlineData("ABORT", true)]
  [InlineData("CANCEL", true)]
  [InlineData("EXCEPTION", true)]
  [InlineData("HARDWARE ERROR", true)]
  [InlineData("COMMUNICATION ERROR", true)]
  [InlineData("TIMEOUT", true)]
  [InlineData("EARLY RETURN", false)]
  public async Task EveryTerminalPathRunsMandatoryEquipmentReset(
    string terminalPath,
    bool terminalStepThrows)
  {
    bool resetCalled = false;

    await ExecutionFinalizer.RunMandatoryStepsAsync(
      (terminalPath, () => terminalStepThrows
        ? Task.FromException(new OperationCanceledException(terminalPath))
        : Task.CompletedTask),
      ("финальный сброс использованного оборудования", () =>
      {
        resetCalled = true;
        return Task.CompletedTask;
      }));

    Assert.True(resetCalled);
  }
}
