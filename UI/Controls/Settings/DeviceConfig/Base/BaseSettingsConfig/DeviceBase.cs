namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Базовый класс для работы с общими настройками устройства.
  /// </summary>
  public class DeviceBase
  {
    private readonly DeviceSettingsControl deviceSettingsControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceBase"/>.
    /// </summary>
    /// <param name="deviceSettingsControl">Элемент управления настройками устройства.</param>
    public DeviceBase(DeviceSettingsControl deviceSettingsControl)
    {
      this.deviceSettingsControl = deviceSettingsControl;
    }

    /// <summary>
    /// Получает значение первой части IP-адреса.
    /// </summary>
    public int IpPart1Value => deviceSettingsControl.IpPart1Value;

    /// <summary>
    /// Получает значение второй части IP-адреса.
    /// </summary>
    public int IpPart2Value => deviceSettingsControl.IpPart2Value;

    /// <summary>
    /// Получает значение третьей части IP-адреса.
    /// </summary>
    public int IpPart3Value => deviceSettingsControl.IpPart3Value;

    /// <summary>
    /// Получает значение четвертой части IP-адреса.
    /// </summary>
    public int IpPart4Value => deviceSettingsControl.IpPart4Value;
  }
}
