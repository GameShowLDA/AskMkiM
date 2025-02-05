using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Device;
using NewCore.Enum;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для ChassisManagerSettings.xaml
  /// </summary>
  public partial class ChassisManagerSettings : UserControl
  {

    public event EventHandler RequestClose;
    public event EventHandler<IChassisManager> DeviceSaved;
    private Dictionary<string, Type> deviceModelMap = new Dictionary<string, Type>();

    private void exit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RequestClose?.Invoke(this, EventArgs.Empty);
    }


    private void save_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (deviceModelComboBox.SelectedItem is string selectedModel)
      {
        var models = ReflectionHelper.GetAllImplementations<IChassisManager>();
        var instance = models
            .Select(t => Activator.CreateInstance(t) as IChassisManager)
            .FirstOrDefault(inst => inst != null && inst.Name == selectedModel);


        if (instance != null)
        {
          if (int.TryParse(textNumber.Text, out int number))
          {
            instance.Number = number;

          }
          DeviceSaved?.Invoke(this, instance);
        }
      }
    }

    private void IpPart_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void IpPart_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        if (int.TryParse(textBox.Text, out int value))
        {
          if (value > 255)
          {
            textBox.Text = "255";
            textBox.CaretIndex = textBox.Text.Length; // Перемещаем курсор в конец
          }
        }
      }
    }


    private void ConnectionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (connectionTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        string selectedType = selectedItem.Content.ToString();
        if (selectedType.ToLower().Contains("ip"))
        {
          ComSettin.Visibility = Visibility.Collapsed;
          BaudRateSettings.Visibility = Visibility.Collapsed;
          FlowControlSettings.Visibility = Visibility.Collapsed;
          ParitySettings.Visibility = Visibility.Collapsed;
          StopBitsSettings.Visibility = Visibility.Collapsed;
          DataBitsSettings.Visibility = Visibility.Collapsed;
          IpSettings.Visibility = Visibility.Visible;
          ipPart3TextBox.Text = deviceNumber.Text;
        }
        else
        {
          IpSettings.Visibility = Visibility.Collapsed;
          LoadComPorts();
          ComSettin.Visibility = Visibility.Visible;
        }
      }
    }

    private void LoadComPorts()
    {
      string[] ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
      comPortComboBox.ItemsSource = ports;
    }

    private void ComPortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (comPortComboBox.SelectedItem is string selectedPort)
      {
        BaudRateSettings.Visibility = Visibility.Visible;
        FlowControlSettings.Visibility = Visibility.Visible;
        ParitySettings.Visibility = Visibility.Visible;
        StopBitsSettings.Visibility = Visibility.Visible;
        DataBitsSettings.Visibility = Visibility.Visible;
        VidSettings.Visibility = Visibility.Visible;
        PidSettings.Visibility = Visibility.Visible;

        (string vid, string pid) = GetVidPidFromComPort(selectedPort);
        VidData.Text = vid;
        PidData.Text = pid;
      }
    }

    /// <summary>
    /// Получает VID и PID по COM-порту
    /// </summary>
    private (string vid, string pid) GetVidPidFromComPort(string portName)
    {
      try
      {
        using (var searcher = new ManagementObjectSearcher(
                   "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%" + portName + "%'"))
        {
          foreach (ManagementObject obj in searcher.Get())
          {
            string deviceID = obj["DeviceID"]?.ToString();
            if (deviceID != null)
            {
              var vid = ExtractValue(deviceID, "VID_");
              var pid = ExtractValue(deviceID, "PID_");
              return (vid, pid);
            }
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка получения VID/PID: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      return ("Не найден", "Не найден");
    }

    /// <summary>
    /// Извлекает значение (VID или PID) из строки DeviceID
    /// </summary>
    private string ExtractValue(string deviceID, string key)
    {
      int startIndex = deviceID.IndexOf(key, StringComparison.OrdinalIgnoreCase);
      if (startIndex >= 0)
      {
        startIndex += key.Length;
        int endIndex = deviceID.IndexOfAny(new char[] { '&', '\\' }, startIndex); // Обрезаем по `&` или `\`
        return endIndex > startIndex ? deviceID[startIndex..endIndex] : deviceID[startIndex..];
      }
      return "Не найден";
    }


    public ChassisManagerSettings()
    {
      InitializeComponent();
      LoadDeviceModels();
      IpSettings.Visibility = Visibility.Collapsed;
      ComSettin.Visibility = Visibility.Collapsed;
      BaudRateSettings.Visibility = Visibility.Collapsed;
      FlowControlSettings.Visibility = Visibility.Collapsed;
      ParitySettings.Visibility = Visibility.Collapsed;
      StopBitsSettings.Visibility = Visibility.Collapsed;
      DataBitsSettings.Visibility = Visibility.Collapsed;
      VidSettings.Visibility = Visibility.Collapsed;
      PidSettings.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Загружает модели устройств, реализующие IChassisManager, в ComboBox, 
    /// используя свойство Name вместо названия класса.
    /// </summary>
    private void LoadDeviceModels()
    {
      var models = ReflectionHelper.GetAllImplementations<IChassisManager>();

      deviceModelMap = models
          .Select(t => Activator.CreateInstance(t) as IChassisManager)
          .Where(instance => instance != null)
          .ToDictionary(instance => instance.Name, instance => instance.GetType());

      deviceModelComboBox.ItemsSource = deviceModelMap.Keys; // Отображаем только имена в ComboBox
    }


    /// <summary>
    /// Обрабатывает выбор устройства в ComboBox.
    /// </summary>
    private void DeviceModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (deviceModelComboBox.SelectedItem is string selectedModel &&
          deviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        try
        {
          Type baseClass = DetermineBaseClass(selectedType);

          MessageBox.Show($"Выбрана модель: {selectedModel}\nБазовый класс: {baseClass.Name}",
                          "Выбор устройства", MessageBoxButton.OK, MessageBoxImage.Information);

          if (baseClass == typeof(DeviceWithIP))
          {
            ComItem.Visibility = Visibility.Collapsed;
            IpItem.Visibility = Visibility.Visible;
          }
          else if (baseClass == typeof(DeviceWithIP))
          {
            IpItem.Visibility = Visibility.Collapsed;
            ComItem.Visibility = Visibility.Visible;
          }
        }
        catch (InvalidOperationException ex)
        {
          MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }


    private Type DetermineBaseClass(Type selectedType)
    {
      bool inheritsIP = typeof(DeviceWithIP).IsAssignableFrom(selectedType);
      bool inheritsCOM = typeof(DeviceWithCOM).IsAssignableFrom(selectedType);

      if (inheritsIP && inheritsCOM)
      {
        throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} наследует сразу оба базовых класса (DeviceWithIP и DeviceWithCOM).");
      }
      else if (inheritsIP)
      {
        return typeof(DeviceWithIP);
      }
      else if (inheritsCOM)
      {
        return typeof(DeviceWithCOM);
      }
      else
      {
        throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} не наследует ни DeviceWithIP, ни DeviceWithCOM.");
      }
    }


    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        if (int.TryParse(textBox.Text, out int value))
        {
          if (value > 250)
          {
            textBox.Text = "250";
            textBox.CaretIndex = textBox.Text.Length; // Курсор в конец
          }
        }
      }
    }

    private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(typeof(string)))
      {
        string pasteText = (string)e.DataObject.GetData(typeof(string));
        if (!Regex.IsMatch(pasteText, "^[0-9]+$"))
        {
          e.CancelCommand();
        }
      }
      else
      {
        e.CancelCommand();
      }
    }

  }
}
