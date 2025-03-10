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

    /// <summary>
    /// Обрабатывает данные устройства, выполняя проверку, присвоение свойств и сохранение.
    /// </summary>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <returns>Возвращает true, если обработка прошла успешно, иначе false.</returns>
    public bool HandleData(object instance)
    {
      if (DefaultSettingControl.DeviceModelSelectionBox.SelectedItem is string selectedModel &&
          DefaultSettingControl.DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        try
        {
          if (!ValidateIRackInstance(instance, selectedType, out IRack data)) return false;

          ValidateAndAssignDeviceNumber(data);
          AssignConnectionDetails(instance, data);

          int numberChassis = GetSelectedChassisNumber();

          SaveRackEntity(data, numberChassis);

          return true;
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка при создании устройства {selectedType.Name}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      return false;
    }

    /// <summary>
    /// Проверяет, является ли объект экземпляром IRack.
    /// </summary>
    /// <param name="instance">Экземпляр для проверки.</param>
    /// <param name="selectedType">Тип, который должен быть реализован.</param>
    /// <param name="data">Выходной параметр, содержащий преобразованный объект IRack.</param>
    /// <returns>True, если объект успешно преобразован, иначе false.</returns>
    private bool ValidateIRackInstance(object instance, Type selectedType, out IRack? data)
    {
      data = instance as IRack;
      if (data == null)
      {
        MessageBox.Show($"Класс {selectedType.Name} не реализует IRack.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }
      return true;
    }

    /// <summary>
    /// Проверяет и присваивает номер устройства.
    /// </summary>
    /// <param name="data">Объект IRack, которому нужно присвоить номер.</param>
    /// <exception cref="Exception">Выбрасывается, если введённое значение не является числом.</exception>
    private void ValidateAndAssignDeviceNumber(IRack data)
    {
      if (!int.TryParse(DefaultSettingControl.DeviceNumberInput.Text, out int number))
      {
        throw new Exception("Реализовать подсветку ошибки для номера устройства");
      }
      data.Number = number;
    }

    /// <summary>
    /// Определяет тип устройства и устанавливает соответствующие параметры подключения.
    /// </summary>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <param name="data">Объект IRack, которому нужно присвоить параметры подключения.</param>
    /// <exception cref="Exception">Выбрасывается, если ввод некорректен.</exception>
    private void AssignConnectionDetails(object instance, IRack data)
    {
      switch (instance)
      {
        case DeviceWithIP deviceWithIP:
          AssignIPAddress(deviceWithIP, data);
          break;
        case DeviceWithCOM deviceWithCOM:
          AssignCOMPort(deviceWithCOM, data);
          break;
        default:
          MessageBox.Show("Устройство не принадлежит к известным типам (DeviceWithIP или DeviceWithCOM).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          throw new Exception("Неизвестный тип устройства");
      }
    }

    /// <summary>
    /// Присваивает IP-адрес устройству типа DeviceWithIP.
    /// </summary>
    /// <param name="deviceWithIP">Экземпляр устройства с IP.</param>
    /// <param name="data">Объект IRack, которому нужно присвоить параметры подключения.</param>
    /// <exception cref="Exception">Выбрасывается, если IP-адрес указан некорректно.</exception>
    private void AssignIPAddress(DeviceWithIP deviceWithIP, IRack data)
    {
      if (!int.TryParse(DefaultSettingControl.IPAddressPart3Input.Text, out int ipPart))
      {
        throw new Exception("Реализовать подсветку ошибки для IP-адреса");
      }
      deviceWithIP.IPAddress = IPAddress.Parse($"192.168.{ipPart}.0");
      data.ConnectionDetails = deviceWithIP.IPAddress.ToString();
    }

    /// <summary>
    /// Присваивает COM-порт устройству типа DeviceWithCOM.
    /// </summary>
    /// <param name="deviceWithCOM">Экземпляр устройства с COM-портом.</param>
    /// <param name="data">Объект IRack, которому нужно присвоить параметры подключения.</param>
    /// <exception cref="Exception">Выбрасывается, если COM-порт не выбран.</exception>
    private void AssignCOMPort(DeviceWithCOM deviceWithCOM, IRack data)
    {
      if (string.IsNullOrEmpty(DefaultSettingControl.COMPortSelectionBox.Text))
      {
        throw new Exception("Реализовать подсветку ошибки для COM-порта");
      }
      data.ConnectionDetails = BaseHandler<IRack>.GetConnectionDetails(DefaultSettingControl, deviceWithCOM);
    }

    /// <summary>
    /// Получает выбранный номер шасси из ChassisModelComboBox.
    /// </summary>
    /// <returns>Число — номер шасси, если выбран корректный вариант.</returns>
    /// <exception cref="Exception">Выбрасывается, если шасси не выбрано.</exception>
    private int GetSelectedChassisNumber()
    {
      if (!int.TryParse(DefaultSettingControl.ChassisModelComboBox.SelectedItem?.ToString(), out int numberChassis))
      {
        MessageBox.Show("Выбранный тестер не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        throw new Exception("Шасси не выбрано или указано некорректно");
      }
      return numberChassis;
    }

    /// <summary>
    /// Создает объект RackEntity и сохраняет его в базе данных.
    /// </summary>
    /// <param name="data">Объект IRack с данными устройства.</param>
    /// <param name="numberChassis">Номер шасси, к которому относится устройство.</param>
    private void SaveRackEntity(IRack data, int numberChassis)
    {
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
    }
  }
}
