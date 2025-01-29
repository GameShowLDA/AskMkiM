namespace AppConfig.Data.Protocol
{
  public class ProtocolModel
  {
    /// <summary>
    /// Отображение данных об устройстве в протоколе.
    /// </summary>
    public bool DeviceInfo { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли сохранять протокол.
    /// </summary>
    public bool SaveProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли печатать протокол.
    /// </summary>
    public bool PrintProtocol { get; set; }

    /// <summary>
    /// Флаг, указывающий время выполнения операций.
    /// </summary>
    public bool StartTime { get; set; }

    /// <summary>
    /// Флаг, указывающий на подробное отображение протокола.
    /// </summary>
    public bool ShowDetailedProtocol { get; set; }
  }
}
