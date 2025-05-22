using System.Windows.Controls;
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
            messageColor: ShowMessageModel.ErrorMessage.TitleColor));
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
            // обработка mint
            break;
        }
      }
      else
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Не удалось получить устройство.",
          messageColor: ShowMessageModel.ErrorMessage.TitleColor));
        return;
      }



      // продолжение работы с meter и device
    }

  }
}
