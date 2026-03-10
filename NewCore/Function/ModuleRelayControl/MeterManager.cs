using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет измерителем модуля коммутации реле (МКР).
  /// </summary>
  public class MeterManager : IMeterManager
  {
    /// <summary>
    /// Экземпляр интерфейса модуля коммутации реле.
    /// </summary>
    private readonly IRelaySwitchModule _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="MeterManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public MeterManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Включает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на включение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> ConnectMeterAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(5, 1);
      await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString());
      return true;
    }

    /// <summary>
    /// Отключает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на отключение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> DisconnectMeterAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(5, 2);
      await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString());
      return true;
    }

    /// <summary>
    /// Получает ответ от измерителя о наличии замыкания шин или точек.
    /// </summary>
    /// <returns><c>true</c>, если есть замыкание; <c>false</c>, если нет.</returns>
    /// <remarks>
    /// Этот метод отправляет команду на проверку состояния измерителя и анализирует его ответ.
    /// </remarks>
    public async Task<bool> GetMeterResponseAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(7);
      var answer = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);

      // TODO : Нормально распарсить ответ МКР.
      var result = answer.Contains("7.1");
      return result;
    }
  }
}
