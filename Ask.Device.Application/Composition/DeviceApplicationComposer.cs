using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Device.Breakdowntester;
using Ask.Device.Runtime.Device.PowerSourceModule;
using Ask.Device.Runtime.Device.RelaySwitchModule;
using Ask.Device.Runtime.Device.SwitchingDevice;
using Ask.Device.Runtime.Device.UninterruptiblePowerSupply;
using DbcCapacitorManagerAdapter = Ask.Device.Application.FunctionAdapters.DeviceBusCommutation.CapacitorManagerAdapter;
using DbcConnectorManagerAdapter = Ask.Device.Application.FunctionAdapters.DeviceBusCommutation.ConnectorManagerAdapter;
using DbcRelayManagerAdapter = Ask.Device.Application.FunctionAdapters.DeviceBusCommutation.RelayManagerAdapter;
using DbcResistorManagerAdapter = Ask.Device.Application.FunctionAdapters.DeviceBusCommutation.ResistorManagerAdapter;
using GptAcwModeAdapter = Ask.Device.Application.FunctionAdapters.GPT.AcwModeAdapter;
using GptDcwModeAdapter = Ask.Device.Application.FunctionAdapters.GPT.DcwModeAdapter;
using GptIrModeAdapter = Ask.Device.Application.FunctionAdapters.GPT.IrModeAdapter;
using GptSystemSettingsAdapter = Ask.Device.Application.FunctionAdapters.GPT.SystemSettingsAdapter;
using MintBusManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent.BusManagerAdapter;
using MintCurrentManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent.CurrentManagerAdapter;
using MintVoltageManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent.VoltageManagerAdapter;
using RelayBusManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleRelayControl.BusManagerAdapter;
using RelayMeterManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleRelayControl.MeterManagerAdapter;
using RelayPointManagerAdapter = Ask.Device.Application.FunctionAdapters.ModuleRelayControl.PointManagerAdapter;
using UpsConnectableManagerAdapter = Ask.Device.Application.FunctionAdapters.MikUps1101rRm.ConnectableManagerAdapter;
using UpsPowerManagerAdapter = Ask.Device.Application.FunctionAdapters.MikUps1101rRm.PowerManagerAdapter;

namespace Ask.Device.Application.Composition
{
  /// <summary>
  /// Выполняет прикладную композицию runtime-устройств, навешивая adapters с повторами,
  /// пользовательскими сообщениями и преобразованием ошибок.
  /// </summary>
  public static class DeviceApplicationComposer
  {
    /// <summary>
    /// Декорирует устройство прикладными adapters, если для его типа предусмотрена композиция.
    /// </summary>
    /// <typeparam name="T">Тип устройства.</typeparam>
    /// <param name="device">Экземпляр runtime-устройства.</param>
    /// <returns>Тот же экземпляр устройства после композиции.</returns>
    public static T Compose<T>(T device)
      where T : class, IDevice
    {
      ArgumentNullException.ThrowIfNull(device);

      switch (device)
      {
        case GPT79904 gpt:
          gpt.AcwManger = new GptAcwModeAdapter(gpt);
          gpt.DcwManger = new GptDcwModeAdapter(gpt);
          gpt.IrManger = new GptIrModeAdapter(gpt);
          gpt.SystemManger = new GptSystemSettingsAdapter(gpt);
          break;

        case ModuleVoltageCurrentSource moduleVoltageCurrentSource:
          moduleVoltageCurrentSource.BusManager = new MintBusManagerAdapter(moduleVoltageCurrentSource);
          moduleVoltageCurrentSource.CurrentManager = new MintCurrentManagerAdapter(moduleVoltageCurrentSource);
          moduleVoltageCurrentSource.VoltageManager = new MintVoltageManagerAdapter(moduleVoltageCurrentSource);
          break;

        case ModuleRelayControl moduleRelayControl:
          moduleRelayControl.BusManager = new RelayBusManagerAdapter(moduleRelayControl);
          moduleRelayControl.MeterManager = new RelayMeterManagerAdapter(moduleRelayControl);
          moduleRelayControl.PointManager = new RelayPointManagerAdapter(moduleRelayControl);
          break;

        case DeviceBusCommutation deviceBusCommutation:
          deviceBusCommutation.ConnectorManager = new DbcConnectorManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.CapacitorManager = new DbcCapacitorManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.RelayManager = new DbcRelayManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.ResistorManager = new DbcResistorManagerAdapter(deviceBusCommutation);
          break;

        case MikUps1101rRmDevice mikUps1101rRmDevice:
          mikUps1101rRmDevice.ConnectableManager = new UpsConnectableManagerAdapter(mikUps1101rRmDevice);
          mikUps1101rRmDevice.PowerManager = new UpsPowerManagerAdapter(mikUps1101rRmDevice);
          break;
      }

      if (device.ConnectableManager is not EquipmentTrackingConnectable)
      {
        device.ConnectableManager = new EquipmentTrackingConnectable(
          device,
          device.ConnectableManager);
      }

      return device;
    }
  }
}
