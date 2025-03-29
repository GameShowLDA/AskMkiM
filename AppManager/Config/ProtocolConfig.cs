using AppManager.Data.Protocol;

namespace AppManager.Config
{
  /// <summary>
  /// Класс конфигурации для <see cref="ProtocolConfig"/>.
  /// </summary>
  public static class ProtocolConfig
  {
    static ProtocolModel ProtocolModel = new ProtocolModel();

    #region Set.

    /// <summary>
    /// Устанавливает отображение информации об устройствах в протоколе.
    /// </summary>
    /// <param name="enable">true для отображения, false для скрытия.</param>
    public static async Task SetDeviceInfo(bool enable)
    {
      await Task.Run(() =>
      {
        ProtocolModel.ShowDeviceInfo = enable;
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
        ProtocolModel.ShowDetailedProtocol = enable;
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
        ProtocolModel.AutoSaveProtocol = enable;
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
        ProtocolModel.AutoPrintProtocol = enable;
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
        ProtocolModel.DisplayOperationTime = enable;
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Возвращает статус отображения информации об устройствах в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetDeviceInfo() => await Task.Run(() => ProtocolModel.ShowDeviceInfo);

    /// <summary>
    /// Возвращает статус отображения подробной информации в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetShowDetailedProtocol() => await Task.Run(() => ProtocolModel.ShowDetailedProtocol);

    /// <summary>
    /// Возвращает статус автосохранения протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetSaveProtocol() => await Task.Run(() => ProtocolModel.AutoSaveProtocol);

    /// <summary>
    /// Возвращает статус авто печати протокола.
    /// </summary>
    /// <returns>true, если включено; false, если выключено.</returns>
    public static async Task<bool> GetPrintProtocol() => await Task.Run(() => ProtocolModel.AutoPrintProtocol);

    /// <summary>
    /// Возвращает статус отображения времени в протоколе.
    /// </summary>
    /// <returns>true, если отображается; false, если скрывается.</returns>
    public static async Task<bool> GetTimeStart() => await Task.Run(() => ProtocolModel.DisplayOperationTime);

    #endregion

    /// <summary>
    /// Перезаписывает конфигурационный файл протокола с текущими настройками.
    /// </summary>
    public static async void RewriteProtocolConfig()
    {
      ProtocolModel protocolModel = new ProtocolModel();
      protocolModel.ShowDeviceInfo = ProtocolModel.ShowDeviceInfo;
      protocolModel.ShowDetailedProtocol = ProtocolModel.ShowDetailedProtocol;
      protocolModel.AutoSaveProtocol = ProtocolModel.AutoSaveProtocol;
      protocolModel.AutoPrintProtocol = ProtocolModel.AutoPrintProtocol;
      protocolModel.DisplayOperationTime = ProtocolModel.DisplayOperationTime;

      ProtocolFileManager protocolFileManager = new ProtocolFileManager(FileLocations.ProtocolConfigPath);
      await protocolFileManager.RewriteFileAsync(protocolModel);
    }
  }
}
