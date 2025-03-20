using System.Net;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Communication;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Класс для управления состоянием модуля источника напряжения и тока (МИНТ).
  /// </summary>
  public class StateManager : IStateManager
  {
    private readonly IPowerSourceModule _moduleVoltageCurrentSource;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleVoltageCurrentSource">Модуль источника напряжения и тока, для которого будет выполняться управление состоянием.</param>
    public StateManager(IPowerSourceModule moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    /// <summary>
    /// Инициализация модуля коммутации реле и проверка корректности его подключения.
    /// </summary>
    /// <returns>Кортеж, где первый элемент — булево значение успешности подключения, второй — строка с сообщением об ошибке или успехе.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), cmd, 2000).ConfigureAwait(true);

      // Десериализация ответа
      BaseResponse baseResponse = BaseResponse.FromJson(result);
      if (baseResponse != null)
      {
        // Проверка на соответствие номеру шасси и устройства
        if (baseResponse.NumberChassis == _moduleVoltageCurrentSource.NumberChassis &&
            baseResponse.NumberDevice == _moduleVoltageCurrentSource.Number)
        {
          return (true, result);
        }
        else
        {
          string errorMessage = string.Empty;

          // Сообщения об ошибках, если номера не совпадают
          if (baseResponse.NumberChassis != _moduleVoltageCurrentSource.NumberChassis)
          {
            errorMessage += $"Несовпадение по NumberChassis: ожидается {_moduleVoltageCurrentSource.NumberChassis}, получено {baseResponse.NumberChassis}. ";
          }

          if (baseResponse.NumberDevice != _moduleVoltageCurrentSource.Number)
          {
            errorMessage += $"Несовпадение по NumberDevice: ожидается {_moduleVoltageCurrentSource.Number}, получено {baseResponse.NumberDevice}.";
          }

          return (false, errorMessage.Trim());
        }
      }

      return (false, result);
    }

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <returns>Возвращает <see cref="Task"/>, представляющий асинхронную операцию сброса.</returns>
    public async Task<bool> ResetAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleVoltageCurrentSource.ConnectionDetails), new DeviceCommand(2));
      return true;
    }
  }
}