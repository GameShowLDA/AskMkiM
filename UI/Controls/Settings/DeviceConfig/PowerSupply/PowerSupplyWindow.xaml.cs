using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.DataBase.Engine.Builder;
using Ask.DataBase.Engine.Static.Devices;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.PowerSupply;

/// <summary>
/// Настраивает внешний источник питания выбранного тестера в общей форме устройств.
/// </summary>
public partial class PowerSupplyWindow : Window
{
  /// <summary>
  /// Действие закрытия формы, размещённой в боковой панели.
  /// </summary>
  public Action? CloseActionOverride { get; set; }

  /// <summary>
  /// Возникает после сохранения конфигурации источника.
  /// </summary>
  public event EventHandler<PowerSupplyDto>? RequestSave;

  /// <summary>
  /// Создаёт форму настройки источника питания.
  /// </summary>
  public PowerSupplyWindow() => InitializeComponent();

  /// <summary>
  /// Передаёт общий контрол настроек в боковую панель.
  /// </summary>
  /// <returns>Контрол настройки модели, номера, IP и TCP-порта.</returns>
  public DeviceSettingsControl DetachSettingsControl()
  {
    Content = null;
    return deviceSettingsWindow;
  }

  /// <summary>
  /// Загружает модели источников и конфигурацию выбранного устройства.
  /// </summary>
  /// <param name="headUnit">Тестер, к которому относится источник.</param>
  /// <param name="editingEntity">Изменяемая конфигурация или null при добавлении.</param>
  public void SetSettings(IHeadUnit headUnit, PowerSupplyDto? editingEntity = null)
  {
    deviceSettingsWindow.NameDevice = "Источник питания";
    deviceSettingsWindow.LoadDeviceModels<IPowerSupply>();
    deviceSettingsWindow.SetHeadUnit(headUnit);
    if (editingEntity != null) deviceSettingsWindow.LoadFromDevice(editingEntity);
    deviceSettingsWindow.RequestClose += (_, _) => CloseForm();
    deviceSettingsWindow.SaveEvent += async (sender, _) =>
    {
      try
      {
        var selectedDevice = deviceSettingsWindow.CreateSelectedDeviceInstance() as IDevice
          ?? throw new InvalidOperationException("Выберите модель источника питания.");
        var dto = new DeviceSettingsProcessorBase().ProcessDevice<PowerSupplyDto>(selectedDevice, deviceSettingsWindow);
        dto.Port = deviceSettingsWindow.GetPowerSupplyPort();
        if (editingEntity != null)
        {
          dto.Id = editingEntity.Id;
          dto.IsHardwareFailureSimulationEnabled = editingEntity.IsHardwareFailureSimulationEnabled;
        }
        var source = DeviceBuilder.Build<IPowerSupply>(dto);
        var saved = editingEntity == null
          ? await PowerSupplies.CreateAsync(source)
          : await PowerSupplies.UpdateAsync(source);
        dto.Id = saved.Id;
        if (editingEntity == null) DeviceConfigNotifications.ShowCreated(dto);
        else DeviceConfigNotifications.ShowUpdated(dto);
        CloseForm();
        RequestSave?.Invoke(sender, dto);
      }
      catch (Exception error)
      {
        Ask.LogLib.LoggerUtility.LogException(error, customMessage: "Сохранение источника питания");
        Message.MessageBoxCustom.Show(error.Message, "Ошибка сохранения данных", image: MessageBoxImage.Error);
      }
    };
  }

  private void CloseForm()
  {
    if (CloseActionOverride != null) CloseActionOverride();
    else Close();
  }
}
