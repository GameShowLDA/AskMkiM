using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using NewCore.Device;
using DbcCapacitorManagerAdapter = NewCore.FunctionAdapters.DeviceBusCommutation.CapacitorManagerAdapter;
using DbcConnectorManagerAdapter = NewCore.FunctionAdapters.DeviceBusCommutation.ConnectorManagerAdapter;
using DbcRelayManagerAdapter = NewCore.FunctionAdapters.DeviceBusCommutation.RelayManagerAdapter;
using DbcResistorManagerAdapter = NewCore.FunctionAdapters.DeviceBusCommutation.ResistorManagerAdapter;
using DbcStateManagerAdapter = NewCore.FunctionAdapters.DeviceBusCommutation.StateManagerAdapter;
using GptAcwModeAdapter = NewCore.FunctionAdapters.GPT.AcwModeAdapter;
using GptConnectableManagerAdapter = NewCore.FunctionAdapters.GPT.ConnectableManagerAdapter;
using GptDcwModeAdapter = NewCore.FunctionAdapters.GPT.DcwModeAdapter;
using GptIrModeAdapter = NewCore.FunctionAdapters.GPT.IrModeAdapter;
using GptSystemSettingsAdapter = NewCore.FunctionAdapters.GPT.SystemSettingsAdapter;
using KeysightAcVoltageMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.AcVoltageMeasurementAdapter;
using KeysightCapacitanceMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.CapacitanceMeasurementAdapter;
using KeysightConnectionAdapter = NewCore.FunctionAdapters.Keysight3466new.KeysightConnectionAdapter;
using KeysightContinuityMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.ContinuityMeasurementAdapter;
using KeysightDcVoltageMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.DcVoltageMeasurementAdapter;
using KeysightDiodeMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.DiodeMeasurementAdapter;
using KeysightResistanceMeasurementAdapter = NewCore.FunctionAdapters.Keysight3466new.ResistanceMeasurementAdapter;
using MintBusManagerAdapter = NewCore.FunctionAdapters.ModuleVoltageCurrent.BusManagerAdapter;
using MintCurrentManagerAdapter = NewCore.FunctionAdapters.ModuleVoltageCurrent.CurrentManagerAdapter;
using MintStateManagerAdapter = NewCore.FunctionAdapters.ModuleVoltageCurrent.StateManagerAdapter;
using MintVoltageManagerAdapter = NewCore.FunctionAdapters.ModuleVoltageCurrent.VoltageManagerAdapter;
using RelayBusManagerAdapter = NewCore.FunctionAdapters.ModuleRelayControl.BusManagerAdapter;
using RelayMeterManagerAdapter = NewCore.FunctionAdapters.ModuleRelayControl.MeterManagerAdapter;
using RelayPointManagerAdapter = NewCore.FunctionAdapters.ModuleRelayControl.PointManagerAdapter;
using RelayStateManagerAdapter = NewCore.FunctionAdapters.ModuleRelayControl.StateManagerAdapter;
using UpsConnectableManagerAdapter = NewCore.FunctionAdapters.MikUps1101rRm.ConnectableManagerAdapter;
using UpsPowerManagerAdapter = NewCore.FunctionAdapters.MikUps1101rRm.PowerManagerAdapter;

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
          gpt.ConnectableManager = new GptConnectableManagerAdapter(gpt);
          break;

        case ModuleVoltageCurrentSource moduleVoltageCurrentSource:
          moduleVoltageCurrentSource.BusManager = new MintBusManagerAdapter(moduleVoltageCurrentSource);
          moduleVoltageCurrentSource.CurrentManager = new MintCurrentManagerAdapter(moduleVoltageCurrentSource);
          moduleVoltageCurrentSource.ConnectableManager = new MintStateManagerAdapter(moduleVoltageCurrentSource);
          moduleVoltageCurrentSource.VoltageManager = new MintVoltageManagerAdapter(moduleVoltageCurrentSource);
          break;

        case ModuleRelayControl moduleRelayControl:
          moduleRelayControl.ConnectableManager = new RelayStateManagerAdapter(moduleRelayControl);
          moduleRelayControl.BusManager = new RelayBusManagerAdapter(moduleRelayControl);
          moduleRelayControl.MeterManager = new RelayMeterManagerAdapter(moduleRelayControl);
          moduleRelayControl.PointManager = new RelayPointManagerAdapter(moduleRelayControl);
          break;

        case DeviceBusCommutation deviceBusCommutation:
          deviceBusCommutation.ConnectableManager = new DbcStateManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.ConnectorManager = new DbcConnectorManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.CapacitorManager = new DbcCapacitorManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.RelayManager = new DbcRelayManagerAdapter(deviceBusCommutation);
          deviceBusCommutation.ResistorManager = new DbcResistorManagerAdapter(deviceBusCommutation);
          break;

        case KeysightDevice keysightDevice:
          keysightDevice.CapacitanceManager = new KeysightCapacitanceMeasurementAdapter(keysightDevice);
          keysightDevice.ConnectableManager = new KeysightConnectionAdapter(keysightDevice);
          keysightDevice.ContinuityManager = new KeysightContinuityMeasurementAdapter(keysightDevice);
          keysightDevice.ResistanceManager = new KeysightResistanceMeasurementAdapter(keysightDevice);
          keysightDevice.AcVoltageManager = new KeysightAcVoltageMeasurementAdapter(keysightDevice);
          keysightDevice.DcVoltageManager = new KeysightDcVoltageMeasurementAdapter(keysightDevice);
          keysightDevice.DiodeManager = new KeysightDiodeMeasurementAdapter(keysightDevice);
          break;

        case MikUps1101rRmDevice mikUps1101rRmDevice:
          mikUps1101rRmDevice.ConnectableManager = new UpsConnectableManagerAdapter(mikUps1101rRmDevice);
          mikUps1101rRmDevice.PowerManager = new UpsPowerManagerAdapter(mikUps1101rRmDevice);
          break;
      }

      return device;
    }
  }
}
