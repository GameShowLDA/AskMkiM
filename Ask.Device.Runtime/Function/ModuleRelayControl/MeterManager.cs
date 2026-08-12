using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing;
using Ask.Device.Runtime.Commands;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет измерителем модуля коммутации реле (МКР).
  /// </summary>
  public class MeterManager : IMeterManager
  {
    private readonly ModuleRelayControlQueryExecutor _queryExecutor;
    private readonly IRelaySwitchModule _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="MeterManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public MeterManager(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl;
      _queryExecutor = new ModuleRelayControlQueryExecutor(moduleRelayControl);
    }

    /// <summary>
    /// Включает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если команда успешно отправлена.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на включение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> ConnectMeterAsync(IUserInteractionService? userMessageService = null)
    {
      DeviceCommand cmd = new DeviceCommand(5, 1);
      string answer = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 1000);
      return await ModuleRelayControlResponseProcessor.CheckMeterOperationAsync(
        answer, _moduleRelayControl, connect: true, userMessageService);
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
      DeviceCommand cmd = new DeviceCommand(5, 2);
      string answer = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 1000);
      return await ModuleRelayControlResponseProcessor.CheckMeterOperationAsync(
        answer, _moduleRelayControl, connect: false, userMessageService);
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
      DeviceCommand cmd = new DeviceCommand(7);
      string answer = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 1000);

      return await ModuleRelayControlResponseProcessor.CheckMeterStateAsync(
        answer, _moduleRelayControl, userMessageService);
    }
  }
}
