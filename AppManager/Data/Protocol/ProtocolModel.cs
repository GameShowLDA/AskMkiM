namespace AppManager.Data.Protocol
{
  public class ProtocolModel
  {
    /// <summary>
    /// Отображение данных об устройстве в протоколе.
    /// </summary>
    public bool ShowDeviceInfo { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли сохранять протокол.
    /// </summary>
    public bool AutoSaveProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли печатать протокол.
    /// </summary>
    public bool AutoPrintProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий время выполнения операций.
    /// </summary>
    public bool DisplayOperationTime { get; set; }

    /// <summary>
    /// Флаг, указывающий на подробное отображение протокола.
    /// </summary>
    public bool ShowDetailedProtocol { get; set; }
  }
}
