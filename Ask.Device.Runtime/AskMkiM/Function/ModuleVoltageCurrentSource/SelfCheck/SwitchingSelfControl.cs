using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.AskMkiM.Function.ModuleVoltageCurrentSource.SelfCheck
{
  /// <summary>
  /// Содержит методы самоконтроля коммутационного устройства.
  /// </summary>
  internal static class SwitchingSelfControl
  {
    /// <summary>
    /// Выполняет проверку коммутации с использованием мультиметра,
    /// источника питания и коммутационного устройства.
    /// </summary>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="fastMeter">Мультиметр, используемый для измерений.</param>
    /// <param name="powerSourceModule">Источник питания.</param>
    /// <param name="switchingDevice">Коммутационное устройство.</param>
    /// <returns>Асинхронная задача выполнения проверки коммутации.</returns>
    static internal async Task CheckSwitching(CancellationToken cancellationToken, IUserInteractionService messageService, IMultimeter fastMeter, IPowerSourceModule powerSourceModule, ISwitchingDevice switchingDevice)
    {
      await SelfTestMessages.PublishInformationAsync("Начало проверки коммутации", messageService);
      await SelfTestMessages.PublishInformationAsync("Настройка оборудования", messageService);
      await powerSourceModule.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply12V, messageService);
      await powerSourceModule.VoltageManager.SetVoltageLevelAsync(5, 0, messageService);
      await Task.Delay(1000);

      var busesA = Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("A") && !bus.ToString().StartsWith("AB") && !bus.ToString().StartsWith("A1"))
                       .ToList();

      var busesB = Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("B") && !bus.ToString().StartsWith("B1"))
                       .ToList();

      await fastMeter.DcVoltageManager.SetDCVoltageModeAsync(messageService);

      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.ConnectBusToNegativeAsync(item, messageService);
      }
      await Task.Delay(1000);

      foreach (var bus in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await SelfTestMessages.PublishInformationAsync("Настройка оборудования", messageService);
      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.DisconnectBusToNegativeAsync(item, messageService);
      }

      foreach (var item in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await powerSourceModule.BusManager.ConnectBusToPositiveAsync(item, messageService);
      }
      await Task.Delay(1000);

      foreach (var bus in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await powerSourceModule.ConnectableManager.ResetAsync(messageService);
    }

    /// <summary>
    /// Выполняет проверку указанной шины коммутационного устройства.
    /// </summary>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="switchingBus">Проверяемая шина.</param>
    /// <param name="switchingDevice">Коммутационное устройство.</param>
    /// <param name="powerSource">Источник питания.</param>
    /// <param name="fastMeter">Мультиметр, используемый для измерения напряжения.</param>
    /// <returns>Асинхронная задача выполнения проверки шины.</returns>
    static private async Task CheckBus(IUserInteractionService messageService, SwitchingBus switchingBus, ISwitchingDevice switchingDevice, IPowerSourceModule powerSource, IMultimeter fastMeter)
    {
      var busSwitch = GetAbPair(switchingBus);
      if (busSwitch == null)
      {
        await SelfTestMessages.PublishInformationAsync(
          $"Не удалось определить шину AB для {switchingBus}",
          messageService);
        return;
      }

      await SelfTestMessages.PublishInformationAsync($"Проверка шины {switchingBus}", messageService);

      var connectBus = await switchingDevice.ConnectorManager.ConnectMultimeter(busSwitch, messageService);

      MeasurementRange measurementRange = new MeasurementRange(5, 0, 50);
      if (switchingBus.ToString().StartsWith("A"))
      {
        await powerSource.BusManager.ConnectBusToPositiveAsync(switchingBus, messageService);
        await Task.Delay(100);

        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(measurementRange, userMessageService: messageService);

        await SelfTestMessages.PublishResultAsync(
          $"Напряжение {result}",
          Math.Abs(result - 5.0) < 0.15,
          messageService,
          indentLevel: 2,
          executionError: false,
          skipPause: false);
        await powerSource.BusManager.DisconnectBusToPositiveAsync(switchingBus, messageService);
      }
      else if (switchingBus.ToString().StartsWith("B"))
      {
        await powerSource.BusManager.ConnectBusToNegativeAsync(switchingBus, messageService);
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(measurementRange, userMessageService: messageService);

        await SelfTestMessages.PublishResultAsync(
          $"Напряжение {result}",
          Math.Abs(result - 5.0) < 0.15,
          messageService,
          indentLevel: 2,
          executionError: false,
          skipPause: false);
        await powerSource.BusManager.DisconnectBusToNegativeAsync(switchingBus, messageService);
      }

      await switchingDevice.ConnectorManager.DisconnectMultimeter(busSwitch, messageService);
    }

    /// <summary>
    /// Возвращает объединённую шину AB, соответствующую указанной шине A или B.
    /// </summary>
    /// <param name="bus">Шина, для которой требуется определить пару AB.</param>
    /// <returns>Соответствующая объединённая шина.</returns>
    /// <exception cref="Exception">
    /// Выбрасывается, если для указанной шины невозможно определить соответствующую шину AB.
    /// </exception>
    private static SwitchingBusNew GetAbPair(SwitchingBus bus)
    {
      if (bus.ToString().StartsWith("A") || bus.ToString().StartsWith("B"))
      {
        var index = bus.ToString().Substring(1); // Например: "1", "2", "3", "4"

        if (Enum.TryParse($"AB{index}", out SwitchingBusNew abBus))
          return abBus;
      }

      throw new Exception("Не удалось разобрать шины"); // Если не удалось сопоставить
    }
  }
}
