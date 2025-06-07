using System.Diagnostics;
using System.Windows.Controls;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Interface.Main;
using UI.Components;
using Utilities.Models;
using static UI.Components.DeviceSelectorPanel;

namespace Mode.SelfControl.DeviceCheck
{
  /// <summary>
  /// Логика взаимодействия для ModuleSelfControl.xaml
  /// </summary>
  public partial class ModuleSelfControl : UserControl
  {
    public ModuleSelfControl()
    {
      InitializeComponent();
      InitializeSettings();
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true, 
        checkPower: false);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      DeviceSelectorPanel deviceSelector = DeviceSelectorHelper.GetInputFieldSafe(ProtocolUI);

      var device = DeviceSelectorHelper.GetSelectedRelayDeviceByTypeSafe(deviceSelector);
      var type = deviceSelector.GetSelectedRelayDeviceType();
      var part = deviceSelector.GetSelectedSelfControlEnumUntypedSafe();

      if (device != null)
      {
        var meter = DeviceSelectorHelper.GetFastMeterSafe(deviceSelector);
        if (meter == null)
        {
          await ProtocolUI.ShowMessageAsync(new ShowMessageModel(
            "Ошибка",
            message: "Не удалось преобразовать объект в измеритель!",
            type: ShowMessageModel.MessageType.Error));
          return;
        }

        switch (type)
        {
          case RelayDeviceType.RelaySwitchModule when device is IRelaySwitchModule relay:
            // обработка relay
            break;

          case RelayDeviceType.SwitchingDevice when device is ISwitchingDevice switcher:
            await switcher.SelfTestManager.StartSelfCheck(ProtocolUI, part, switcher, meter);
            break;

          case RelayDeviceType.PowerSourceModule when device is IPowerSourceModule mint:
            var numberChassis = mint.NumberChassis;
            var switcher1 = new SwitchingDeviceServices().GetDevicesByNumberChassis(numberChassis).FirstOrDefault();
            await mint.SelfTestManager.StartSelfCheck(ProtocolUI.GetCancellationToken(), ProtocolUI, part, switcher1, mint, meter);
            // обработка mint
            break;
        }
      }
      else
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Не удалось получить устройство.",
          type: ShowMessageModel.MessageType.Error));
        return;
      }
    }
  }
}
