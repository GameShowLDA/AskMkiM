using Ask.Core.Shared.DTO.Executor;
using Ask.UI.Features.ProtocolNew.Protocol;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Protocol;

public sealed class InspectionProtocolBuilderTests
{
  [Fact(DisplayName = "Каждая ошибка выводится отдельной строкой заключения")]
  public void Build_WhenSeveralErrorsExist_WritesEachErrorSeparately()
  {
    var settings = new ActionSettings
    {
      StartDelegate = (_, _, _, _) => Task.CompletedTask,
      Name = "Самоконтроль модуля МКР-350 1.4",
    };
    settings.ExecutionErrors.Add("Точка[20] - Отключение с шины A");
    settings.ExecutionErrors.Add("Точка[20] - Отключение с шины B");

    var result = new InspectionProtocolBuilder().Build(settings);

    Assert.Contains("1. Точка[20] - Отключение с шины A [БРАК]", result);
    Assert.Contains("2. Точка[20] - Отключение с шины B [БРАК]", result);
  }
}
