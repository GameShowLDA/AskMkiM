using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.UnitTests.TestInfrastructure;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using Moq;

namespace Ask.Engine.UnitTests.Metrology;

/// <summary>
/// Проверяет безопасный расчёт диапазона метрологического допуска.
/// </summary>
public sealed class MeasurementToleranceCalculatorTests
{
  /// <summary>
  /// Проверяет публикацию ошибки и отсутствие результата для значения вне настроенного диапазона.
  /// </summary>
  [Fact]
  public async Task TryCalculateAsync_ValueBelowConfiguredRange_PublishesErrorAndReturnsNull()
  {
    await WpfTestHost.RunAsync(async () =>
    {
      ShowMessageModel? publishedMessage = null;
      var outputService = CreateOutputService(message => publishedMessage = message);

      var result = await MeasurementToleranceCalculator.TryCalculateAsync(
        MeasurementTypeCommand.EHT,
        0.05,
        outputService.Object);

      Assert.Null(result);
      Assert.NotNull(publishedMessage);
      Assert.Equal("Ошибка расчёта метрологического допуска", publishedMessage.Header);
      Assert.Equal(
        "Не удалось определить диапазон погрешности для команды EHT",
        publishedMessage.Message);
      Assert.Equal(ShowMessageModel.MessageType.Error, publishedMessage.Status);
    });
  }

  /// <summary>
  /// Проверяет возврат рассчитанного допуска без публикации ошибки.
  /// </summary>
  [Fact]
  public async Task TryCalculateAsync_ConfiguredValue_ReturnsToleranceWithoutError()
  {
    var outputService = CreateOutputService(_ => { });

    var result = await MeasurementToleranceCalculator.TryCalculateAsync(
      MeasurementTypeCommand.EHT,
      0.5,
      outputService.Object);

    Assert.NotNull(result);
    Assert.Equal(0.45, result.Value.LowerBound, precision: 10);
    Assert.Equal(0.55, result.Value.UpperBound, precision: 10);
    Assert.Equal(0.05, result.Value.Delta, precision: 10);
    outputService.Verify(
      service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()),
      Times.Never());
  }

  private static Mock<IMessageOutputService> CreateOutputService(Action<ShowMessageModel> callback)
  {
    var outputService = new Mock<IMessageOutputService>();
    outputService
      .Setup(service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Callback<ShowMessageModel, bool, bool, bool, bool, string, string, int>(
        (message, _, _, _, _, _, _, _) => callback(message))
      .Returns(Task.CompletedTask);
    return outputService;
  }
}
