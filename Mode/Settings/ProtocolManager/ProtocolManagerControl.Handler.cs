using static AppConfig.Config.ProtocolConfig;

namespace Mode.Settings.ProtocolManager
{
  public partial class ProtocolManagerControl
  {
    /// <summary>
    /// Флаг, указывающий, начата ли работа с конфигурацией
    /// </summary>
    readonly bool start = false;

    /// <summary>
    /// Устанавливает конфигурацию протокола на основе данных из YAML-файла
    /// </summary>
    private async void SetConfiguration()
    {
      deviceData.IsChecked = await GetDeviceInfo();
      save.IsChecked = await GetSaveProtocol();
      print.IsChecked = await GetPrintProtocol();
      startTime.IsChecked = await GetTimeStart();
      showDetailedProtocol.IsChecked = await GetShowDetailedProtocol();
    }

    /// <summary>
    /// Сохраняет новые данные конфигурации протокола
    /// </summary>
    private async Task NewDataSaveAsync()
    {
      if (start)
      {
        await SetDeviceInfo((bool)deviceData.IsChecked);
        await SetSaveProtocol((bool)save.IsChecked);
        await SetPrintProtocol((bool)print.IsChecked);
        await SetTimeStart((bool)startTime.IsChecked);
        await SetShowDetailedProtocol((bool)showDetailedProtocol.IsChecked);

        RewriteProtocolConfig();
      }
    }
  }
}
