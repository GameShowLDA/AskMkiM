using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppConfig.DataBase.Services;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.Base.BaseSettings
{
  /// <summary>
  /// Логика взаимодействия для BaseSettingsControl.xaml
  /// </summary>
  public partial class BaseSettingsControl : UserControl
  {
    /// <summary>
    /// Инициализирует компонент и загружает начальные настройки.
    /// </summary>
    public BaseSettingsControl()
    {
      InitializeComponent();
      LoadControl();

      IsIpPart3Enabled = false;
      IsIpPart4Enabled = false;
      IsChassisNumberEnabled = true;
      IsRackNumberEnabled = true;
    }

    /// <summary>
    /// Свойство для добавления дополнительных настроек из других элементов управления.
    /// </summary>
    public UIElement AdditionalSettings
    {
      get { return (UIElement)GetValue(AdditionalSettingsProperty); }
      set { SetValue(AdditionalSettingsProperty, value); }
    }

    /// <summary>
    /// Свойство зависимости для хранения дополнительных настроек.
    /// </summary>
    public static readonly DependencyProperty AdditionalSettingsProperty =
        DependencyProperty.Register("AdditionalSettings", typeof(UIElement), typeof(BaseSettingsControl), new PropertyMetadata(null, OnAdditionalSettingsChanged));

    /// <summary>
    /// Обработчик изменений свойства <see cref="AdditionalSettings"/>.
    /// Обновляет содержимое контейнера дополнительных настроек.
    /// </summary>
    private static void OnAdditionalSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is BaseSettingsControl control)
      {
        control.AdditionalSettingsContainer.Content = e.NewValue;
      }
    }

    /// <summary>
    /// Загружает начальные настройки интерфейса, скрывая ненужные элементы.
    /// </summary>
    private void LoadControl()
    {
      DeviceSettingsBorder.Visibility = Visibility.Collapsed;
      ConnectionSettingsBlock.Visibility = Visibility.Collapsed;
      RacksNumberBorder.Visibility = Visibility.Collapsed;
    }

    public void LoadDeviceModels<T>() where T : class
    {
      var models = ReflectionHelper.GetAllImplementations<T>();

      var deviceModelMap = models
          .Select(t => Activator.CreateInstance(t) as T)
          .Where(instance => instance != null)
          .ToDictionary(instance => instance.GetType().GetProperty("Name")?.GetValue(instance)?.ToString(), instance => instance.GetType());

      DeviceModelMap = deviceModelMap;
      DeviceModelSelectionBox.ItemsSource = deviceModelMap.Keys;

      var dataManager = new ChassisManagerRepository(AppConfig.Config.SystemStateManager.Context).GetAll();
      ChassisModelsComboBox.ItemsSource = dataManager.Select(d => d.Number).ToList();

      var dataRack = new RackRepository(AppConfig.Config.SystemStateManager.Context).GetAll();
      RacksModelComboBox.ItemsSource = dataRack.Select(d => d.Number).OrderBy(n => n).ToList();
    }
  }
}
