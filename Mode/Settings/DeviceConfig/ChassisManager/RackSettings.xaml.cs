using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net;
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
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Microsoft.IdentityModel.Tokens;
using Mode.Settings.DeviceConfig.Base;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для RackSettings.xaml
  /// </summary>
  public partial class RackSettings : UserControl, IDataProcessor
  {
    /// <summary>
    /// Событие, вызываемое при запросе на закрытие окна настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при запросе на сохранение настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler<RackEntity> RequestSave;

    private ComboBox _chassisBox;

    public RackSettings()
    {
      InitializeComponent();
      LoadDeviceModels();
      DefaultSettingControl.IsIpPart3Enabled = true;
      DefaultSettingControl.IpPart4Content = 0;
      DefaultSettingControl.IsRackNumberEnabled = false;
      DefaultSettingControl.LoadDeviceModels<IRack>();

      DefaultSettingControl.RequestClose += (s, a) => RequestClose?.Invoke(s, a);
      DefaultSettingControl.RequestSave += DefaultSettingControl_RequestSave;
    }

    private void DefaultSettingControl_RequestSave(object? sender, EventArgs e)
    {
      if (DefaultSettingControl.DeviceModelSelectionBox.SelectedItem is string selectedModel)
      {
        BaseHandler<IRack>.ProcessDeviceData(selectedModel, DefaultSettingControl.DeviceModelMap, this);
      }
    }

    /// <summary>
    /// Загружает модели устройств, реализующие IRack, в ComboBox, 
    /// используя свойство Name вместо названия класса.
    /// </summary>
    private void LoadDeviceModels()
    {
      var models = ReflectionHelper.GetAllImplementations<IRack>();

      DefaultSettingControl.DeviceModelMap = models
          .Select(t => Activator.CreateInstance(t) as IRack)
          .Where(instance => instance != null)
          .ToDictionary(instance => instance.Name, instance => instance.GetType());

      DefaultSettingControl.DeviceModelSelectionBox.ItemsSource = DefaultSettingControl.DeviceModelMap.Keys;

      var data = new ChassisManagerRepository(AppConfig.Config.SystemStateManager.Context).GetAll();
      DefaultSettingControl.ChassisModelsComboBox.ItemsSource = data.Select(d => d.Number).ToList();
    }

    /// <summary>
    /// Поиск элемента по Tag внутри UserControl
    /// </summary>
    private T FindControl<T>(DependencyObject parent, string tag) where T : FrameworkElement
    {
      if (parent == null)
      {
        Console.WriteLine("⚠️ FindControl: Parent == null!");
        return null;
      }

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      Console.WriteLine($"🔍 FindControl: Проверяю {parent.GetType().Name}, Количество детей: {childrenCount}");

      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);

        if (child is T control && control.Tag?.ToString() == tag)
        {
          Console.WriteLine($"✅ Найден {typeof(T).Name} с Tag={tag}");
          return control;
        }

        var result = FindControl<T>(child, tag);
        if (result != null) return result;
      }

      return null;
    }

    public bool HandleData(object instance)
    {
      if (DefaultSettingControl.DeviceModelSelectionBox.SelectedItem is string selectedModel)
      {
        if (DefaultSettingControl.DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
        {
          try
          {
            var data = instance as IRack;
            if (data == null)
            {
              MessageBox.Show($"Класс {selectedType.Name} не реализует IRack.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return false;
            }

            if (int.TryParse(DefaultSettingControl.DeviceNumberInput.Text, out int number))
            {
              data.Number = number;
            }
            else
            {
              throw new Exception("Реализовать подсветку ошибки для номера устройства");
            }

            if (instance is DeviceWithIP deviceWithIP)
            {
              if (int.TryParse(DefaultSettingControl.IPAddressPart3Input.Text, out int ipPart))
              {
                deviceWithIP.IPAddress = IPAddress.Parse($"192.168.{ipPart}.0");
                data.ConnectionDetails = deviceWithIP.IPAddress.ToString();
              }
              else
              {
                throw new Exception("Реализовать подсветку ошибки для IP-адреса");
              }
            }
            else if (instance is DeviceWithCOM deviceWithCOM)
            {
              if (!string.IsNullOrEmpty(DefaultSettingControl.COMPortSelectionBox.Text))
              {
                data.ConnectionDetails = BaseHandler<IRack>.GetConnectionDetails(DefaultSettingControl, instance);
              }
              else
              {
                throw new Exception("Реализовать подсветку ошибки для COM-порта");
              }
            }
            else
            {
              MessageBox.Show("Устройство не принадлежит к известным типам (DeviceWithIP или DeviceWithCOM).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return false;
            }

            int numberChassis;

            if (!string.IsNullOrEmpty(_chassisBox.SelectedItem.ToString()))
            {
              numberChassis = int.Parse(_chassisBox.SelectedItem.ToString());
            }
            else
            {
              MessageBox.Show("Выбранный тестер не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return false;
            }

            var chassisEntity = new RackEntity
            {
              Name = data.Name,
              Description = data.Description,
              Number = data.Number,
              ConnectionDetails = data.ConnectionDetails,
              NumberChassis = numberChassis
            };

            new RackRepository(AppConfig.Config.SystemStateManager.Context).Create(chassisEntity);
            RequestSave?.Invoke(this, chassisEntity);
            return true;

          }
          catch (Exception ex)
          {
            MessageBox.Show($"Ошибка при создании устройства {selectedType.Name}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
      }

      return false;
    }
  }

}
