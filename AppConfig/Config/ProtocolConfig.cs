using AppConfig.Data.Protocol;

namespace AppConfig.Config
{
  /// <summary>
  /// Класс конфигурации для <see cref="ProtocolConfig"/>.
  /// </summary>
  public static class ProtocolConfig
  {
    #region Properties.

    /// <summary>
    /// Флаг, указывающий, нужно ли отображать информацию об устройствах.
    /// </summary>
    static private bool ShowDeviceInfo { get; set; }

    /// <summary>
    /// Флаг, указывающий, на режим подробного вывода информации в протокол.
    /// </summary>
    static private bool ShowDetailedProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий, включено ли автоматическое сохранение протокола.
    /// </summary>
    static private bool AutoSaveProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий, включена ли автоматическая печать протокола.
    /// </summary>
    static private bool AutoPrintProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли отображать время выполнения операций.
    /// </summary>
    static private bool DisplayOperationTime { get; set; }
    #endregion

    #region Set.

    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetDeviceInfo(bool enable)
    {
      await Task.Run(() =>
      {
        ShowDeviceInfo = enable;
      });
    }

    /// <summary>
    /// Устанавливает режим подробного вывода информации в протокол.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    static public async Task SetShowDetailedProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        ShowDetailedProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает автосохранение протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetSaveProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        AutoSaveProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает автоматическую печать протокола.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetPrintProtocol(bool enable)
    {
      await Task.Run(() =>
      {
        AutoPrintProtocol = enable;
      });
    }

    /// <summary>
    /// Устанавливает отображение времени выполнения операций.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetTimeStart(bool enable)
    {
      await Task.Run(() =>
      {
        DisplayOperationTime = enable;
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает статус отображения информации об устройствах в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetDeviceInfo() => await Task.Run(() => ShowDeviceInfo);

    /// <summary>
    /// Возвращает статус отображения подробной информации в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetShowDetailedProtocol() => await Task.Run(() => ShowDetailedProtocol);

    /// <summary>
    /// Возвращает статус автосохранения протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetSaveProtocol() => await Task.Run(() => AutoSaveProtocol);

    /// <summary>
    /// Возвращает статус авто печати протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetPrintProtocol() => await Task.Run(() => AutoPrintProtocol);


    /// <summary>
    /// Возвращает статус отображения времени в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetTimeStart() => await Task.Run(() => DisplayOperationTime);

    #endregion

    public static async void RewriteProtocolConfig()
    {
      ProtocolModel protocolModel = new ProtocolModel();
      protocolModel.DeviceInfo = ShowDeviceInfo;
      protocolModel.ShowDetailedProtocol = ShowDetailedProtocol;
      protocolModel.SaveProtocol = AutoSaveProtocol;
      protocolModel.PrintProtocol = AutoPrintProtocol;
      protocolModel.StartTime = DisplayOperationTime;

      ProtocolFileManager protocolFileManager = new ProtocolFileManager(FileLocations.ProtocolConfigPath);
      await protocolFileManager.RewriteFileAsync(protocolModel);
    }
  }
}
