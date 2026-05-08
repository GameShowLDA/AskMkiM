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
    /// Метод для проверки попадания результата в допустимый диапазон и отображения соответствующего сообщения пользователю.
    /// </summary>
    /// <param name="idealResult">Результат, который должен получиться в идеале</param>
    /// <param name="result">Полученный результат</param>
    /// <param name="param">Название параметра измерений (сопротивление, напряжение и т.п.)</param>
    /// <param name="userMessageService">Пользовательский интерфейс для вывода</param>
    /// <returns></returns>
    public static async Task IsCorrectRangeAsync(double idealResult, double result, string param, IUserInteractionService? userMessageService = null)
    {
      if (InRange(idealResult, result))
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

    private static bool InRange(double idealResult, double result)
    {
      double error_rate = (0.01 * result) + 0.02;
      if (result - error_rate <= idealResult && result + error_rate >= idealResult) return true;
      return false;
    }
  }
}