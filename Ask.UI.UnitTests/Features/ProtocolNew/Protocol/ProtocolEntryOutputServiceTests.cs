using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Features.ProtocolNew.Protocol;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Protocol;

public sealed class ProtocolEntryOutputServiceTests
{
  [Fact(DisplayName = "Итог самоконтроля не дублирует внутреннюю ошибку измерения мультиметра")]
  public void ShouldSkipAccumulatedError_WhenSelfTestMeasurementResult_ReturnsTrue()
  {
    var message = new ShowMessageModel
    {
      Header = "Результат \"Измерение электрической ёмкости\"",
    };

    var result = ProtocolEntryOutputService.ShouldSkipAccumulatedError(message, CheckType.SelfTest);

    Assert.True(result);
  }

  [Fact(DisplayName = "Итог самоконтроля не дублирует промежуточную ошибку измерения мультиметра")]
  public void ShouldSkipAccumulatedError_WhenSelfTestDeviceMeasurementResult_ReturnsTrue()
  {
    var message = new ShowMessageModel
    {
      Header = "Keysight 34465A(1.16) - Измерение ёмкости",
    };

    var result = ProtocolEntryOutputService.ShouldSkipAccumulatedError(message, CheckType.SelfTest);

    Assert.True(result);
  }

  [Fact(DisplayName = "Итог самоконтроля оставляет ошибку теста")]
  public void ShouldSkipAccumulatedError_WhenSelfTestCheckResult_ReturnsFalse()
  {
    var message = new ShowMessageModel
    {
      Header = "Тест 150 Ом (± 5%)",
    };

    var result = ProtocolEntryOutputService.ShouldSkipAccumulatedError(message, CheckType.SelfTest);

    Assert.False(result);
  }

  [Fact(DisplayName = "Итог обычного теста оставляет ошибку измерения")]
  public void ShouldSkipAccumulatedError_WhenRegularTestMeasurementResult_ReturnsFalse()
  {
    var message = new ShowMessageModel
    {
      Header = "Результат \"Измерение электрического сопротивления\"",
    };

    var result = ProtocolEntryOutputService.ShouldSkipAccumulatedError(message, CheckType.Test);

    Assert.False(result);
  }
}
