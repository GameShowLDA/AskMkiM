using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.UI.Features.EquipmentConfiguration.PowerSupply;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

/// <summary>
/// Подключает форму параметров внешнего источника питания к общим настройкам устройства.
/// </summary>
public partial class DeviceSettingsControl
{
  private PowerSupplySettingsControl? _powerSupplySettingsControl;

  /// <summary>
  /// Возвращает TCP-порт из дополнительных параметров источника.
  /// </summary>
  public int GetPowerSupplyPort() => _powerSupplySettingsControl?.Port ?? 0;

  /// <summary>
  /// Записывает параметры источника из формы в конфигурацию.
  /// </summary>
  /// <param name="dto">Конфигурация внешнего источника питания.</param>
  public void ApplyPowerSupplySettings(PowerSupplyDto dto)
  {
    ArgumentNullException.ThrowIfNull(dto);
    (_powerSupplySettingsControl ?? throw new InvalidOperationException(
      "Форма параметров источника питания не инициализирована.")).ApplyTo(dto);
  }

  private void LoadPowerSupplySettings(PowerSupplyDto dto)
    => _powerSupplySettingsControl?.LoadFrom(dto);

  private void ShowPowerSupplyAdditionalSettings()
  {
    _powerSupplySettingsControl = new PowerSupplySettingsControl();
    AdditionalSettingsContainer.Content = _powerSupplySettingsControl;
    foreach (var source in _powerSupplySettingsControl.ValidationSources)
      RegisterValidationSource(source, _powerSupplySettingsControl.ValidationSection);
  }
}
