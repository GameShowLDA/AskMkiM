using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Static.Devices;
using System.Windows;
using UI.Controls.Settings.DeviceConfig.Base;
using UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig;

namespace UI.Controls.Settings.DeviceConfig.UninterruptiblePowerSupply
{
  /// <summary>
  /// Interaction logic for UninterruptiblePowerSupplyWindow.xaml.
  /// </summary>
  public partial class UninterruptiblePowerSupplyWindow : Window, IDataProcessor
  {
    public Action? CloseActionOverride { get; set; }
    private UninterruptiblePowerSupplyEntity? _editingEntity;

    /// <summary>
    /// Close request event.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Save request event.
    /// </summary>
    public event EventHandler<UninterruptiblePowerSupplyEntity> RequestSave;

    /// <summary>
    /// Window constructor.
    /// </summary>
    public UninterruptiblePowerSupplyWindow()
    {
      InitializeComponent();
    }

    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);

    public DeviceSettingsControl DetachSettingsControl()
    {
      Content = null;
      return deviceSettingsWindow;
    }

    public void ProcessData(IDevice device, DeviceSettingsControl control)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Initializes settings screen for UPS add/edit.
    /// </summary>
    public void SetSettings(object? sender, IHeadUnit e, UninterruptiblePowerSupplyEntity? editingEntity = null)
    {
      _editingEntity = editingEntity;
      deviceSettingsWindow.NameDevice = "Бесперебойник";
      deviceSettingsWindow.LoadDeviceModels<IUninterruptiblePowerSupply>();
      deviceSettingsWindow.SetHeadUnit(e);
      if (editingEntity != null)
      {
        deviceSettingsWindow.LoadFromDevice(editingEntity);
      }

      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        var processor = new DeviceSettingsProcessorBase();
        var baseDevice = deviceSettingsWindow.CreateSelectedDeviceInstance();

        UninterruptiblePowerSupplyEntity deviceEntity = processor.ProcessDevice<UninterruptiblePowerSupplyEntity>(
            selectedDevice: baseDevice as IDevice,
            control: deviceSettingsWindow,
            additionalDataProcessor: this);

        if (deviceEntity != null)
        {
          deviceEntity.LastResolvedDevicePath = (baseDevice as IUninterruptiblePowerSupply)?.LastResolvedDevicePath ?? string.Empty;

          try
          {
            if (_editingEntity == null)
            {
              UninterruptiblePowerSupplies.CreateAsync(deviceEntity).GetAwaiter().GetResult();
            }
            else
            {
              deviceEntity.Id = _editingEntity.Id;
              UninterruptiblePowerSupplies.UpdateAsync(deviceEntity).GetAwaiter().GetResult();
            }

            RequestSave?.Invoke(s, deviceEntity);
            RequestCloseWindow();
          }
          catch (DuplicateEntityException ex)
          {
            Message.MessageBoxCustom.Show(ex.Message, "Ошибка сохраненения данных", image: MessageBoxImage.Error);
            throw;
          }
        }
      };

      deviceSettingsWindow.RequestClose += (s, a) =>
      {
        RequestClose?.Invoke(s, a);
        RequestCloseWindow();
      };
    }

    private void RequestCloseWindow()
    {
      if (CloseActionOverride != null)
      {
        CloseActionOverride.Invoke();
        return;
      }

      Close();
    }
  }
}
