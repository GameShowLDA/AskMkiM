using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Newtonsoft.Json.Linq;
using YamlDotNet.Core.Tokens;

namespace Ask.Device.Runtime.Function.Multimeter.SelfCheck
{
  public static class SelfTestHelper
  {

    /// <summary>
    /// Метод для вывода сообщения пользователю о результатах измерения.
    /// </summary>
    /// <param name="status">Статус измерения (true - в норме, false - брак)</param>
    /// <param name="result">Полученный результат</param>
    /// <param name="param">Название параметра измерений (сопротивление, напряжение и т.п.)</param>
    /// <param name="unit">Единица измерения результата</param>
    /// <param name="userMessageService">Пользовательский интерфейс для вывода</param>
    public static async Task IsCorrectRangeAsync(bool status, double result, string param, string? unit = null, IUserInteractionService? userMessageService = null)
    {
      var formattedResult = string.IsNullOrWhiteSpace(unit)
        ? $"{result.ToString("0.000###;-0.000###;0.000")}"
        : $"{result.ToString("0.000###;-0.000###;0.000")} {unit}";

      if (status)
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: $"{formattedResult} [НОРМА]",
            type: ShowMessageModel.MessageType.Success));
      }
      else
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: $"{formattedResult} [БРАК]",
            type: ShowMessageModel.MessageType.Error));
      }
    }

    /// <summary>
    /// Метод для выявления правильности результата с учетом погрешности.
    /// </summary>
    /// <param name="idealResult">Идеальный результат</param>
    /// <param name="result">Получившийся результат</param>
    /// <param name="range">Допустимый диапазон отклонений</param>
    /// <returns><see langword="true"/> - результат находится в допустимом диапазоне</returns>
    /// <remarks>
    /// Определение правилости результата работает по формуле:
    /// <paramref name="result"/> +- <paramref name="range"/> ~ <paramref name="idealResult"/>
    /// </remarks>
    public static bool InRange(double idealResult, double result, double range = 0)
    {
      if (result - range <= idealResult && result + range >= idealResult) return true;
      return false;
    }
  }
}
