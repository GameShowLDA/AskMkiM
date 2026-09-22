using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Engine.Tests.Metrology.MeasurementSystem
{
  /// <summary>
  /// Рассчитывает допустимый диапазон измерения и публикует ошибки расчёта в протокол.
  /// </summary>
  internal static class MeasurementToleranceCalculator
  {
    /// <summary>
    /// Пытается рассчитать допустимый диапазон измерения.
    /// </summary>
    /// <param name="type">Тип метрологической команды.</param>
    /// <param name="measuredValue">Значение, для которого рассчитывается допуск.</param>
    /// <param name="outputService">Сервис вывода сообщений в экранный протокол.</param>
    /// <returns>
    /// Рассчитанные границы и погрешность либо <see langword="null"/>, если расчёт завершился ошибкой.
    /// </returns>
    internal static async Task<(double LowerBound, double UpperBound, double Delta)?> TryCalculateAsync(
      MeasurementTypeCommand type,
      double measuredValue,
      IMessageOutputService outputService)
    {
      try
      {
        return MeasurementErrorDefaults.CalculateToleranceRange(type, measuredValue);
      }
      catch (SystemExceptionBase exception)
      {
        await MetrologyMessages.PublishToleranceCalculationErrorAsync(
          exception.Description,
          outputService);
        return null;
      }
    }
  }
}
