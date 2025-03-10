using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceSettingsControl.xaml
  /// </summary>
  public partial class DeviceSettingsControl : UserControl
  {
    public DeviceSettingsControl()
    {
      InitializeComponent();
      VisibilityElements();

    }
    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
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
    }

    private void VisibilityElements()
    {
      DeviceNumberContainer.Visibility = Visibility.Collapsed;
      ConnectionTypeContainer.Visibility = Visibility.Collapsed;
      IPAddressContainer.Visibility = Visibility.Collapsed;
      COMContainer.Visibility = Visibility.Collapsed;
      AdditionalSettingsContainer.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Определяет базовый класс для указанного типа устройства.
    /// Проверяет, наследуется ли класс от <see cref="DeviceWithIP"/> или <see cref="DeviceWithCOM"/>.
    /// </summary>
    /// <param name="selectedType">Тип устройства, для которого определяется базовый класс.</param>
    /// <returns>Тип базового класса (<see cref="DeviceWithIP"/> или <see cref="DeviceWithCOM"/>).</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если класс наследует оба базовых класса или ни один из них.
    /// </exception>
    private Type GetBaseDeviceType(Type selectedType)
    {
      bool inheritsIP = typeof(DeviceWithIP).IsAssignableFrom(selectedType);
      bool inheritsCOM = typeof(DeviceWithCOM).IsAssignableFrom(selectedType);

      return (inheritsIP, inheritsCOM) switch
      {
        (true, true) => throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} наследует сразу оба базовых класса (DeviceWithIP и DeviceWithCOM)."),
        (true, false) => typeof(DeviceWithIP),
        (false, true) => typeof(DeviceWithCOM),
        _ => throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} не наследует ни DeviceWithIP, ни DeviceWithCOM.")
      };
    }

    private void ShowIP()
    {
      IPAddressContainer.Visibility = Visibility.Visible;
      IpPart1.Text = "192";
      IpPart2.Text = "168";
      IpPart3.Text = _headUnit.Number.ToString();
      IpPart4.Text = DeviceNumberTextBox.Text;
    }

    public T GetDevice<T>() where T : class, IDevice
    {
      MessageBox.Show("Тест данных");
      return default;
    }
  }
}
