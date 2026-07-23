using Ask.Core.Services.UI;
using Ask.UI.Features.ProtocolNew.Execution;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionFinalizerTests
{
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
}
