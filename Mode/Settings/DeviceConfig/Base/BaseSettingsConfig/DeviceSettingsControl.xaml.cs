using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using NewCore.Base.Device;
using NewCore.Base.Interface.Additionally;
using NewCore.Device;

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

    public Type GetBaseDeviceType()
    {
      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
            !DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
        return null;

      return GetBaseDeviceType(selectedType);
    }

    private void ShowIP()
    {
      IPAddressContainer.Visibility = Visibility.Visible;
      IpPart1.Text = "192";
      IpPart2.Text = "168";
      if (_headUnit == null)
      {
        IpPart3.Text = DeviceNumberTextBox.Text;
        IpPart4.Text = "0";
      }
      else
      {
        IpPart3.Text = _headUnit.Number.ToString();
        IpPart4.Text = DeviceNumberTextBox.Text;
      }
    }

    /// <summary>
    /// Создаёт и возвращает экземпляр выбранного пользователем устройства.
    /// </summary>
    /// <returns>Экземпляр конкретного класса устройства, выбранного в DeviceModelSelectionBox.</returns>
    public object CreateSelectedDeviceInstance()
    {
      if (DeviceModelSelectionBox.SelectedItem == null)
        throw new InvalidOperationException("Не выбрана модель устройства!");

      Type selectedType = DeviceModelMap[DeviceModelSelectionBox.SelectedItem.ToString()];

      return Activator.CreateInstance(selectedType);
    }

    /// <summary>
    /// Отображает доступные COM порты.
    /// </summary>
    private void PopulateCOMPorts()
    {
      // Получаем список доступных COM-портов
      string[] portNames = SerialPort.GetPortNames();

      // Привязываем список к ComboBox
      COMPortSelectionBox.ItemsSource = portNames;

      // Если список не пуст, выбираем первый порт по умолчанию
      if (portNames.Any())
      {
        COMPortSelectionBox.SelectedIndex = 0;
      }
    }

    /// <summary>
    /// Получает значения VID и PID для указанного COM-порта и отображает их в текстовых полях.
    /// </summary>
    /// <param name="comPort">Имя COM-порта, например "COM3".</param>
    private void GetVidPidForPort(string comPort)
    {
      string query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%({comPort})%'";

      using (var searcher = new ManagementObjectSearcher(query))
      {
        foreach (ManagementObject device in searcher.Get())
        {
          // Получаем строку DeviceID, где обычно содержатся VID и PID
          string deviceId = device["DeviceID"] as string;
          if (!string.IsNullOrEmpty(deviceId))
          {
            // Ищем шаблон "VID_XXXX&PID_XXXX"
            Regex regex = new Regex(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
            Match match = regex.Match(deviceId);
            if (match.Success)
            {
              // Извлекаем VID и PID
              string vid = match.Groups[1].Value;
              string pid = match.Groups[2].Value;

              // Записываем данные в TextBox-ы
              VIDData.Text = vid;
              PIDData.Text = pid;
              return;
            }
          }
        }
      }

      VIDData.Text = "N/A";
      PIDData.Text = "N/A";
    }

    /// <summary>
    /// Применяет COM-настройки из выбранной модели устройства к элементам управления.
    /// Если свойство присутствует в модели, ищет его значение среди вариантов ComboBox и выбирает его.
    /// Если свойства нет или значение не найдено – оставляет значение по умолчанию.
    /// </summary>
    /// <param name="deviceModel">Экземпляр модели устройства, выбранного пользователем.</param>
    private void ApplyCOMSettingsFromModel(object deviceModel)
    {
      Type modelType = deviceModel.GetType();

      // Обновляем настройки COM: BaudRate, StopBits, DataBits, Parity, FlowControl
      SetComboBoxValueFromProperty(modelType, deviceModel, "BaudRate", BaudRateSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "StopBits", StopBitsSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "DataBits", DataBitsSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "Parity", ParitySelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "FlowControl", FlowControlSelectionBox);
    }

    /// <summary>
    /// Проверяет, содержит ли указанная модель устройства свойство с именем propertyName,
    /// и если да, получает его значение, пытается установить его в ComboBox.
    /// </summary>
    /// <param name="modelType">Тип модели устройства.</param>
    /// <param name="deviceModel">Экземпляр модели устройства.</param>
    /// <param name="propertyName">Имя свойства, например "BaudRate".</param>
    /// <param name="comboBox">ComboBox для установки значения.</param>
    private void SetComboBoxValueFromProperty(Type modelType, object deviceModel, string propertyName, ComboBox comboBox)
    {
      var property = modelType.GetProperty(propertyName);
      if (property != null)
      {
        var valueObj = property.GetValue(deviceModel);
        if (valueObj != null)
        {
          string value = valueObj.ToString();
          // Поиск подходящего элемента в ComboBox
          foreach (var item in comboBox.Items)
          {
            // Если элемент – строка или ComboBoxItem, то сравниваем их содержимое
            string itemContent = item is ComboBoxItem cbItem ? cbItem.Content.ToString() : item.ToString();
            if (string.Equals(itemContent, value, StringComparison.OrdinalIgnoreCase))
            {
              comboBox.SelectedItem = item;
              return;
            }
          }
        }
      }
    }
  }
}
