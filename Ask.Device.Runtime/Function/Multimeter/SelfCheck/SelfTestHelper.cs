using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// <param name="userMessageService">Пользовательский интерфейс для вывода</param>
    public static async Task IsCorrectRangeAsync(bool status, double result, string param, IUserInteractionService? userMessageService = null)
    {
      if (status)
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: $"{result} [НОРМА]",
            type: ShowMessageModel.MessageType.Success)
          );
      }
      else
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: $"{result} [БРАК]",
            type: ShowMessageModel.MessageType.Error)
          );
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