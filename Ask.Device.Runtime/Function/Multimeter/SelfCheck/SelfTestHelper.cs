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
    /// <param name="rangeFrom">Начало диапазона (включительно)</param>
    /// <param name="rangeTo">Конец диапазона (включительно)</param>
    /// <param name="result">Сравниваемый результат измерений</param>
    /// <param name="param">Название параметра измерений (сопротивление, напряжение и т.п.)</param>
    /// <param name="userMessageService">Пользовательский интерфейс для вывода</param>
    public static async Task IsCorrectRangeAsync(double rangeFrom, double rangeTo, double result, string param, IUserInteractionService? userMessageService = null)
    {
      if (result >= rangeFrom && result <= rangeTo)
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: "[НОРМА]",
            type: ShowMessageModel.MessageType.Success)
          );
      }
      else
      {
        await userMessageService.ShowMessageAsync(
          new ShowMessageModel(
            header: $"Тест {param}",
            message: "[БРАК]",
            type: ShowMessageModel.MessageType.Error)
          );
      }
    }
  }
}
