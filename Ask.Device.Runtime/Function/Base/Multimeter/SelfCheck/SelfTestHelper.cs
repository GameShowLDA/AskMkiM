using System;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck
{
  public static class SelfTestHelper
  {
    private const double RelativeErrorMarker = -1;

    /// <summary>
    /// Метод для вывода сообщения пользователю о результатах измерения.
    /// </summary>
    /// <param name="status">Статус измерения (<see langword="true"/> - в норме, <see langword="false"/> - брак).</param>
    /// <param name="result">Полученный результат.</param>
    /// <param name="param">Название параметра измерений (сопротивление, напряжение и т.п.).</param>
    /// <param name="unit">Единица измерения результата.</param>
    /// <param name="idealResult">Идеальный результат.</param>
    /// <param name="percentageError">Процент погрешности от идеального результата.</param>
    /// <param name="userMessageService">Пользовательский интерфейс для вывода.</param>
    public static Task IsCorrectRangeAsync(bool status, double result, string param, string unit, double idealResult, int percentageError, IUserInteractionService? userMessageService = null)
    {
      ArgumentNullException.ThrowIfNull(userMessageService);

      var resultType = status
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error;

      return userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"Тест {param}{unit} {FormatFallibility(idealResult, percentageError)}",
          message: $"{FormatResult(result)} ",
          type: resultType)
        {
          IndentLevel = 1,
        });
    }

    /// <summary>
    /// Метод для выявления правильности результата с учетом погрешности.
    /// </summary>
    /// <param name="idealResult">Идеальный результат.</param>
    /// <param name="result">Получившийся результат.</param>
    /// <param name="range">Допустимый диапазон отклонений.</param>
    /// <returns><see langword="true"/> - результат находится в допустимом диапазоне.</returns>
    /// <remarks>
    /// Определение правильности результата работает по формуле:
    /// <paramref name="result"/> +- <paramref name="range"/> ~ <paramref name="idealResult"/>.
    /// </remarks>
    public static bool InRange(double idealResult, double result, double range = 0)
    {
      return Math.Abs(result - idealResult) <= Math.Abs(range);
    }

    private static string FormatResult(double result)
    {
      return MeasurementValueFormatter.IsOverloadValue(result)
        ? "Overload"
        : MeasurementValueFormatter.Round(result).ToString("N0");
    }

    private static string FormatFallibility(double idealResult, int percentageError)
    {
      return idealResult == RelativeErrorMarker
        ? $"(± {percentageError}%)"
        : $"({idealResult} ± {percentageError}%)";
    }
  }
}