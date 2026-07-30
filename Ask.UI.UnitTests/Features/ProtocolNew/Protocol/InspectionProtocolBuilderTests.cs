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
      StartDelegate = (_, _, _, _, _) => Task.CompletedTask,
      Name = "Самоконтроль модуля МКР-350 1.4",
    };
    settings.ExecutionErrors.Add("Точка[20] - Отключение с шины A");
    settings.ExecutionErrors.Add("Точка[20] - Отключение с шины B");

    var result = new InspectionProtocolBuilder().Build(settings);

    Assert.Contains("1. Точка[20] - Отключение с шины A [БРАК]", result);
    Assert.Contains("2. Точка[20] - Отключение с шины B [БРАК]", result);
  }

  [Fact(DisplayName = "Входные параметры выводятся перед заключением")]
  public void Build_WhenInputParametersExist_WritesThemBeforeConclusion()
  {
    var settings = new ActionSettings
    {
      StartDelegate = (_, _, _, _, _) => Task.CompletedTask,
      Name = "СИ - Метод узла",
    };
    settings.InputParameters.Add("Первая точка: 1.2.3");
    settings.InputParameters.Add("Вторая точка: 1.2.4");

    var result = new InspectionProtocolBuilder().Build(settings);

    var inputIndex = result.IndexOf("Введённые данные:", StringComparison.Ordinal);
    var conclusionIndex = result.IndexOf("Заключение:", StringComparison.Ordinal);

    Assert.True(inputIndex >= 0);
    Assert.True(conclusionIndex > inputIndex);
    Assert.Contains("\tПервая точка: 1.2.3", result);
    Assert.Contains("\tВторая точка: 1.2.4", result);
  }
}
