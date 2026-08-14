using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Reflection;

namespace Ask.UI.Features.ProtocolNew.Protocol;

internal static class ExecutionProtocolEnvironmentSnapshotFactory
{
  public static ExecutionProtocolEnvironmentSnapshot Create(
    ActionSettings action,
    IReadOnlyList<IDevice> equipment)
  {
    var execution = ExecutionConfig.GetExecutionModelSnapshot();
    var protocol = ProtocolConfig.GetProtocolModel();
    var display = DeviceDisplayConfig.GetDeviceDisplayModel();
    var settings = new SortedDictionary<string, string>
    {
      ["Выполнение.Холостой режим"] = State(execution.IdleModeExecution),
      ["Выполнение.Симуляция брака"] = execution.ErroneousMeasurementType.ToString(),
      ["Выполнение.Симуляция аппаратных ошибок"] = State(execution.IsHardwareErrorSimulationMode),
      ["Выполнение.Пошаговый режим"] = State(execution.StepByStepMode),
      ["Выполнение.Остановка при ошибке"] = State(execution.StopOnError),
      ["Выполнение.Legacy-совместимость"] = State(execution.LegacyCompatibilityMode),
      ["Выполнение.Проверка питания отключена"] = State(execution.DisablePowerCheck),
      ["Запуск.Требуется проверка питания"] = State(action.CheckPower),
      ["Протокол.Информация об устройствах"] = State(protocol.ShowDeviceInfo),
      ["Протокол.Заголовочная информация"] = State(protocol.ShowHeaderInfo),
      ["Протокол.Детальный вывод"] = State(protocol.ShowDetailedProtocol),
      ["Протокол.Время операций"] = State(protocol.DisplayOperationTime),
      ["Протокол.Заголовки команд"] = State(protocol.ShowCommandHeadersInProtocol),
      ["Протокол.Шаги проверок"] = State(protocol.ShowTestStepMessagesInProtocol),
      ["Оборудование.Машинные адреса"] = State(display.ShowMachineAddresses),
      ["Оборудование.Коммутация"] = State(display.ShowConnectionInfo),
      ["Оборудование.Параметры выполнения"] = State(display.ShowDeviceExecutionParameters),
      ["Оборудование.Результаты измерений"] = State(display.ShowMeasurementResults),
      ["Оборудование.Промежуточные результаты"] = State(display.ShowIntermediateMeasurementResults)
    };

    return new ExecutionProtocolEnvironmentSnapshot(
      DateTime.Now,
      Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "не определена",
      RoleAuthorizationConfig.CurrentRole?.ToString() ?? "не авторизован",
      action.Name,
      action.CheckType.ToString(),
      action.Mode,
      settings,
      equipment.Select(CreateDeviceSnapshot).ToArray());
  }

  private static ExecutionProtocolDeviceSnapshot CreateDeviceSnapshot(IDevice device) => new(
    device.Id,
    device.Number,
    device.DeviceType.ToString(),
    device.Name,
    device.Description,
    device.ConnectionDetails,
    device.GetType().FullName ?? device.GetType().Name,
    device.DeviceClass,
    device.ConnectionInfo == null
      ? "не определено"
      : $"{device.ConnectionInfo.GetConnectionStatus()}; "
        + $"тип={device.ConnectionInfo.ConnectionType}; "
        + $"connected={device.ConnectionInfo.IsConnected}");

  private static string State(bool enabled) => enabled ? "ВКЛ" : "ВЫКЛ";
}
