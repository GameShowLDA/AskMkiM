using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Interface;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mode.Settings.DeviceConfig.BaseSettings
{
  partial class BaseSettingsControl
  {
    #region COM.

    /// <summary>
    /// Загрузка ком портов.
    /// </summary>
    private void LoadComPorts()
    {
      string[] ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
      comPortComboBox.ItemsSource = ports;
    }

    /// <summary>
    /// Получает VID и PID по COM-порту
    /// </summary>
    private (string vid, string pid) GetVidPidFromComPort(string portName)
    {
      try
      {
        string deviceID = GetDeviceIdByPortName(portName);
        if (!string.IsNullOrEmpty(deviceID))
        {
          return ExtractVidAndPid(deviceID);
        }
      }
      catch (Exception ex)
      {
        ShowErrorMessage($"Ошибка получения VID/PID: {ex.Message}");
      }
      return ("Не найден", "Не найден");
    }

    /// <summary>
    /// Возвращает DeviceID для заданного COM-порта.
    /// </summary>
    private string GetDeviceIdByPortName(string portName)
    {
      using (var searcher = CreatePnPEntitySearcher(portName))
      {
        return RetrieveDeviceId(searcher);
      }
    }

    /// <summary>
    /// Создает объект поиска устройств по имени COM-порта.
    /// </summary>
    private ManagementObjectSearcher CreatePnPEntitySearcher(string portName)
    {
      string query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{portName}%'";
      return new ManagementObjectSearcher(query);
    }

    /// <summary>
    /// Извлекает DeviceID из результатов поиска.
    /// </summary>
    private string RetrieveDeviceId(ManagementObjectSearcher searcher)
    {
      foreach (ManagementObject obj in searcher.Get())
      {
        return obj["DeviceID"]?.ToString();
      }
      return null;
    }

    /// <summary>
    /// Извлекает значения VID и PID из строки DeviceID.
    /// </summary>
    private (string vid, string pid) ExtractVidAndPid(string deviceID)
    {
      string vid = ExtractValue(deviceID, "VID_");
      string pid = ExtractValue(deviceID, "PID_");
      return (vid, pid);
    }

    /// <summary>
    /// Извлекает значение (VID или PID) из строки DeviceID по ключу.
    /// </summary>
    private string ExtractValue(string deviceID, string key)
    {
      int startIndex = deviceID.IndexOf(key, StringComparison.OrdinalIgnoreCase);
      if (startIndex >= 0)
      {
        startIndex += key.Length;
        int endIndex = deviceID.IndexOfAny(new char[] { '&', '\\' }, startIndex);
        return endIndex > startIndex ? deviceID[startIndex..endIndex] : deviceID[startIndex..];
      }
      return "Не найден";
    }

    #endregion

    #region Save.

    /// <summary>
    /// Основной метод для сохранения устройства.
    /// </summary>
    private void SaveDevice()
    {
      if (deviceModelComboBox.SelectedItem is not string selectedModel) return;

      if (!deviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        ShowErrorMessage("Выбранная модель устройства не найдена в карте моделей.");
        return;
      }

      try
      {
        var deviceInstance = CreateDeviceInstance(selectedType);
        if (deviceInstance == null) return;

        var chassisEntity = BuildChassisManagerEntity(deviceInstance);
        if (chassisEntity == null) return;

        SaveChassisEntityToDatabase(chassisEntity);
        DeviceSaved?.Invoke(this, chassisEntity);
      }
      catch (Exception ex)
      {
        ShowErrorMessage($"Ошибка при создании устройства {selectedType.Name}: {ex.Message}");
      }
    }

    /// <summary>
    /// Создает экземпляр устройства и проверяет реализацию IChassisManager.
    /// </summary>
    private IChassisManager CreateDeviceInstance(Type selectedType)
    {
      var instance = Activator.CreateInstance(selectedType);
      if (instance is not IChassisManager data)
      {
        ShowErrorMessage($"Класс {selectedType.Name} не реализует IChassisManager.");
        return null;
      }

      if (!SetDeviceNumber(data)) return null;

      if (!ConfigureDeviceConnection(instance, data)) return null;

      return data;
    }

    /// <summary>
    /// Устанавливает номер устройства.
    /// </summary>
    private bool SetDeviceNumber(IChassisManager data)
    {
      if (int.TryParse(deviceNumber.Text, out int number))
      {
        data.Number = number;
        return true;
      }
      else
      {
        HighlightInputError(deviceNumber, "Неверный номер устройства.");
        return false;
      }
    }

    /// <summary>
    /// Конфигурирует соединение устройства (IP или COM).
    /// </summary>
    private bool ConfigureDeviceConnection(object instance, IChassisManager data)
    {
      if (instance is DeviceWithIP deviceWithIP)
      {
        return ConfigureIPDevice(deviceWithIP, data);
      }
      else if (instance is DeviceWithCOM deviceWithCOM)
      {
        return ConfigureCOMDevice(deviceWithCOM, data);
      }
      else
      {
        ShowErrorMessage("Устройство не принадлежит к известным типам (DeviceWithIP или DeviceWithCOM).");
        return false;
      }
    }

    /// <summary>
    /// Конфигурирует устройство с IP-адресом.
    /// </summary>
    private bool ConfigureIPDevice(DeviceWithIP deviceWithIP, IChassisManager data)
    {
      if (int.TryParse(ipPart3TextBox.Text, out int ipPart))
      {
        deviceWithIP.IPAddress = IPAddress.Parse($"192.168.{ipPart}.0");
        data.ConnectionDetails = deviceWithIP.IPAddress.ToString();
        return true;
      }
      else
      {
        HighlightInputError(ipPart3TextBox, "Неверный IP-адрес.");
        return false;
      }
    }

    /// <summary>
    /// Конфигурирует устройство с COM-портом.
    /// </summary>
    private bool ConfigureCOMDevice(DeviceWithCOM deviceWithCOM, IChassisManager data)
    {
      if (!string.IsNullOrEmpty(comPortComboBox.Text))
      {
        deviceWithCOM.COMPort = new SerialPort(comPortComboBox.Text)
        {
          BaudRate = 9600,
          Parity = Parity.None,
          DataBits = 8,
          StopBits = StopBits.One
        };
        data.ConnectionDetails = deviceWithCOM.COMPort.PortName;
        return true;
      }
      else
      {
        HighlightInputError(comPortComboBox, "Не выбран COM-порт.");
        return false;
      }
    }

    /// <summary>
    /// Создает объект ChassisManagerEntity для сохранения.
    /// </summary>
    private ChassisManagerEntity BuildChassisManagerEntity(IChassisManager data)
    {
      return new ChassisManagerEntity
      {
        Name = data.Name,
        Description = data.Description,
        Number = data.Number,
        ConnectionDetails = data.ConnectionDetails
      };
    }

    /// <summary>
    /// Сохраняет объект ChassisManagerEntity в базу данных.
    /// </summary>
    private void SaveChassisEntityToDatabase(ChassisManagerEntity chassisEntity)
    {
      new ChassisManagerRepository(AppConfig.Config.SystemStateManager.Context).Create(chassisEntity);
    }

    /// <summary>
    /// Подсвечивает ошибочный ввод в TextBox или ComboBox.
    /// </summary>
    private void HighlightInputError(Control control, string errorMessage)
    {
      control.BorderBrush = Brushes.Red;
      control.ToolTip = errorMessage;
      ShowErrorMessage(errorMessage);
    }
    #endregion

    /// <summary>
    /// Показывает сообщение об ошибке.
    /// </summary>
    private void ShowErrorMessage(string message)
    {
      MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
