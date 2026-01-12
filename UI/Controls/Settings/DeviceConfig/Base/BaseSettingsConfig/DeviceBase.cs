using System.IO.Ports;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Базовый класс для работы с настройками устройства.
  /// </summary>
  public class DeviceBase
  {
    /// <summary>
    /// Экземпляр элемента управления настройками устройства.
    /// </summary>
    private readonly DeviceSettingsControl deviceSettingsControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceBase"/>.
    /// </summary>
    /// <param name="deviceSettingsControl">Элемент управления настройками устройства.</param>
    public DeviceBase(DeviceSettingsControl deviceSettingsControl)
    {
      this.deviceSettingsControl = deviceSettingsControl;
    }

    #region Properties

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

    /// <summary>
    /// Получает значение скорости передачи данных.
    /// </summary>
    public int BaudRateValue => deviceSettingsControl.BaudRateValue;

    /// <summary>
    /// Получает значение количества бит данных.
    /// </summary>
    public int DataBitsValue => deviceSettingsControl.DataBitsValue;

    /// <summary>
    /// Получает значение контроля четности.
    /// </summary>
    public Parity ParityValue => deviceSettingsControl.ParityValue;

    /// <summary>
    /// Получает значение количества стоп-битов.
    /// </summary>
    public StopBits StopBitsValue => deviceSettingsControl.StopBitsValue;

    /// <summary>
    /// Получает название порта.
    /// </summary>
    public string PortName => deviceSettingsControl.PortName;

    #endregion
  }
}
