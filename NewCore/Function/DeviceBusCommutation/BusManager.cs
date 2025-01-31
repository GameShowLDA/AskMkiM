using NewCore.Communication;
using NewCore.Device;
using System;
using System.Threading.Tasks;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс для управления шинами и коммутацией устройств через УКШ (Устройство Коммутации Шин).
  /// </summary>
  public class BusManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public BusManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Подключает указанную цепочку УКШ.
    /// </summary>
    /// <param name="numberBlock">Номер блока.</param>
    /// <param name="numberChain">Номер цепочки.</param>
    /// <returns><c>true</c>, если команда успешно отправлена; иначе <c>false</c>.</returns>
    /// <remarks>
    /// Отправляет команду с кодом <c>4, numberBlock, numberChain, 1</c>.
    /// </remarks>
    public async Task<bool> ConnectChainCircuit(int numberBlock, int numberChain)
    {
      DeviceCommand command = new DeviceCommand(4, numberBlock, numberChain, 1);
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
      return true;
    }

    /// <summary>
    /// Отключает указанную цепочку УКШ.
    /// </summary>
    /// <param name="numberBlock">Номер блока.</param>
    /// <param name="numberChain">Номер цепочки.</param>
    /// <returns><c>true</c>, если команда успешно отправлена; иначе <c>false</c>.</returns>
    /// <remarks>
    /// Отправляет команду с кодом <c>4, numberBlock, numberChain, 2</c>.
    /// </remarks>
    public async Task<bool> DisconnectChainCircuit(int numberBlock, int numberChain)
    {
      DeviceCommand command = new DeviceCommand(4, numberBlock, numberChain, 2);
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
      return true;
    }

    /// <summary>
    /// Замыкает шину УКШ для выполнения самоконтроля МКР.
    /// </summary>
    /// <param name="numberBus">Номер шины (символ).</param>
    /// <returns><c>true</c>, если команда успешно отправлена; иначе <c>false</c>.</returns>
    /// <remarks>
    /// Отправляет команду с кодом <c>3, number, 1, 0</c>.
    /// Если номер шины некорректен, возвращает <c>false</c>.
    /// </remarks>
    public async Task<bool> ConnectBusSelfControlAsync(char numberBus)
    {
      if (int.TryParse(numberBus.ToString(), out int number))
      {
        DeviceCommand command = new DeviceCommand(3, number, 1, 0);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }
      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    /// <summary>
    /// Отключает шину УКШ после выполнения самоконтроля МКР.
    /// </summary>
    /// <param name="numberBus">Номер шины (символ).</param>
    /// <returns><c>true</c>, если команда успешно отправлена; иначе <c>false</c>.</returns>
    /// <remarks>
    /// Отправляет команду с кодом <c>3, number, 2, 0</c>.
    /// Если номер шины некорректен, возвращает <c>false</c>.
    /// </remarks>
    public async Task<bool> DisconnectBusSelfControlAsync(char numberBus)
    {
      if (int.TryParse(numberBus.ToString(), out int number))
      {
        DeviceCommand command = new DeviceCommand(3, number, 2, 0);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }
      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    /// <summary>
    /// Выполняет замыкание шины УКШ с указанными параметрами.
    /// </summary>
    /// <param name="connector">Разъем для подключения к шинам.</param>
    /// <param name="bus">Коммутационная шина УКШ.</param>
    /// <param name="lowVoltage">Флаг низковольтной шины (<c>true</c> — низковольтная, <c>false</c> — высоковольтная).</param>
    /// <param name="polarityReversed">Флаг переполюсовки шины (<c>true</c> — переполюсовка, <c>false</c> — обычный режим).</param>
    /// <returns>Ответ от устройства или сообщение об ошибке.</returns>
    public async Task<string> ConnectBusAsync(MeterConnector connector, SwitchingBus bus, bool lowVoltage, bool polarityReversed)
    {
      string action = polarityReversed ? "переполюсовка" : "обычный режим";
      if (!TryGetConnectorNumber(connector, out int connectorNumber) || !TryGetBusNumber(bus, out int busNumber) || !TryGetBusType(bus, out int busType))
      {
        return LogError("Ошибка данных шины или разъема!");
      }

      string voltageType = lowVoltage ? "низковольтную" : "высоковольтную";
      busNumber += lowVoltage ? 0 : 10;

      var command = new DeviceCommand(5, (connectorNumber * 10) + busType, busNumber, !polarityReversed ? 11 : 12);
      LogInformation($"Команда: \"{command}\". Замыкаем {voltageType} шину {bus} на разъеме {connector} в режиме \"{action}\".");

      return await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command, 1000).ConfigureAwait(false);
    }

    /// <summary>
    /// Выполняет размыкание шины УКШ с указанными параметрами.
    /// </summary>
    /// <param name="connector">Разъем для подключения к шинам.</param>
    /// <param name="bus">Шина УКШ.</param>
    /// <param name="lowVoltage">Флаг низковольтной шины (<c>true</c> — низковольтная, <c>false</c> — высоковольтная).</param>
    /// <param name="polarityReversed">Флаг переполюсовки шины (<c>true</c> — переполюсовка, <c>false</c> — обычный режим).</param>
    /// <returns>Ответ от устройства или сообщение об ошибке.</returns>
    public async Task<string> DisconnectBusAsync(MeterConnector connector, SwitchingBus bus, bool lowVoltage, bool polarityReversed)
    {
      string action = polarityReversed ? "переполюсовка" : "обычный режим";
      if (!TryGetConnectorNumber(connector, out int connectorNumber) || !TryGetBusNumber(bus, out int busNumber) || !TryGetBusType(bus, out int busType))
      {
        return LogError("Ошибка данных шины или разъема!");
      }

      string voltageType = lowVoltage ? "низковольтную" : "высоковольтную";
      busNumber += lowVoltage ? 0 : 10;

      var command = new DeviceCommand(5, (connectorNumber * 10) + busType, busNumber, !polarityReversed ? 21 : 22);
      LogInformation($"Команда: {command}. Размыкаем {voltageType} шину {bus} на разъеме {connector} в режиме \"{action}\".");

      return await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command, 1000).ConfigureAwait(false);
    }

    /// <summary>
    /// Преобразует тип разъема в числовой номер.
    /// </summary>
    /// <param name="connector">Тип разъема.</param>
    /// <param name="connectorNumber">Выходной параметр, содержащий номер разъема.</param>
    /// <returns><c>true</c>, если преобразование успешно; иначе <c>false</c>.</returns>
    private bool TryGetConnectorNumber(MeterConnector connector, out int connectorNumber)
    {
      switch (connector)
      {
        case MeterConnector.XS3:
          connectorNumber = 1;
          return true;
        case MeterConnector.XS4:
          connectorNumber = 2;
          return true;
        case MeterConnector.XS5:
          connectorNumber = 3;
          return true;
        default:
          connectorNumber = -1;
          return false;
      }
    }

    /// <summary>
    /// Извлекает номер шины из её имени.
    /// </summary>
    /// <param name="bus">Тип шины.</param>
    /// <param name="busNumber">Выходной параметр, содержащий номер шины.</param>
    /// <returns><c>true</c>, если номер успешно получен; иначе <c>false</c>.</returns>
    private bool TryGetBusNumber(SwitchingBus bus, out int busNumber)
    {
      string busName = bus.ToString();
      busNumber = -1;
      foreach (char ch in busName)
      {
        if (char.IsDigit(ch))
        {
          return int.TryParse(busName.Substring(busName.IndexOf(ch)), out busNumber);
        }
      }
      return false;
    }

    /// <summary>
    /// Определяет тип шины (A, B, AB) на основе её имени.
    /// </summary>
    /// <param name="bus">Тип шины.</param>
    /// <param name="busType">Выходной параметр, содержащий тип шины (1 — A, 2 — B, 3 — AB).</param>
    /// <returns><c>true</c>, если тип успешно определён; иначе <c>false</c>.</returns>
    private bool TryGetBusType(SwitchingBus bus, out int busType)
    {
      busType = -1;
      if (bus is SwitchingBus.A1 || bus is SwitchingBus.A2 || bus is SwitchingBus.A3 || bus is SwitchingBus.A4)
      {
        busType = 1;
      }
      else if (bus is SwitchingBus.B1 || bus is SwitchingBus.B2 || bus is SwitchingBus.B3 || bus is SwitchingBus.B4)
      {
        busType = 2;
      }
      else if (bus is SwitchingBus.AB1 || bus is SwitchingBus.AB2 || bus is SwitchingBus.AB3 || bus is SwitchingBus.AB4)
      {
        busType = 3;
      }
      return busType != -1;
    }

  }
}