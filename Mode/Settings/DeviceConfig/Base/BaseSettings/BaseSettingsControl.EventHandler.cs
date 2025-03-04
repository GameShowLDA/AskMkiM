using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NewCore.Base;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.IO.Ports;
using YamlDotNet.Core.Tokens;

namespace Mode.Settings.DeviceConfig.Base.BaseSettings
{
  public partial class BaseSettingsControl
  {

    #region Валидация кода.

    /// <summary>
    /// Проверяет, что ввод содержит только цифры (0-9) для частей IP-адреса.
    /// Запрещает ввод любых других символов.
    /// </summary>
    /// <param name="sender">Источник события (обычно TextBox).</param>
    /// <param name="e">Аргументы события, содержащие введенный текст.</param>
    private void ValidateIPAddressInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");

    /// <summary>
    /// Ограничивает вводимое значение в текстовом поле IP-адреса диапазоном 0-255.
    /// Если введенное число больше 255, автоматически заменяет его на 255.
    /// </summary>
    /// <param name="sender">Источник события (обычно TextBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении текста.</param>
    private void RestrictIPAddressValue(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox && int.TryParse(textBox.Text, out int value) && value > 255)
      {
        textBox.Text = "255";
        textBox.CaretIndex = textBox.Text.Length;
      }
    }

    /// <summary>
    /// Обрабатывает вставку данных в поле номера устройства, 
    /// позволяя вставлять только допустимые значения.
    /// </summary>
    /// <param name="sender">Источник события (обычно TextBox).</param>
    /// <param name="e">Аргументы события, содержащие данные о вставляемом тексте.</param>
    private void ValidateDeviceNumberOnPaste(object sender, DataObjectPastingEventArgs e)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Проверяет вводимые символы в поле номера устройства, 
    /// разрешая ввод только допустимых значений.
    /// </summary>
    /// <param name="sender">Источник события (обычно TextBox).</param>
    /// <param name="e">Аргументы события, содержащие введенный текст.</param>
    private void ValidateDeviceNumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");

    #endregion

    #region Обработчики выбора модели устройства и шасси.

    /// <summary>
    /// Обрабатывает выбор модели шасси в выпадающем списке.
    /// Отображает блок выбора типа устройства.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении выбора.</param>
    private void HandleChassisModelSelection(object sender, SelectionChangedEventArgs e)
    {
      if (IsRackNumberEnabled)
      {
        RacksNumberBorder.Visibility = Visibility.Visible;
      }
      else
      {
        DeviceTypeBorder.Visibility = Visibility.Visible;
      }
    }

    /// <summary>
    /// Обрабатывает выбор модели стойки коммутационной в выпадающем списке.
    /// Отображает блок выбора типа устройства.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении выбора.</param>
    private void HandleRackModelSelection(object sender, SelectionChangedEventArgs e) => DeviceTypeBorder.Visibility = Visibility.Visible;

    /// <summary>
    /// Обрабатывает изменение выбранной модели устройства в выпадающем списке.
    /// Определяет базовый класс выбранного устройства и настраивает доступные типы подключения.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении выбора.</param>
    private void HandleDeviceModelSelection(object sender, SelectionChangedEventArgs e)
    {
      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
          !DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
        return;

      try
      {
        Type baseClass = GetBaseDeviceType(selectedType);

        ConnectionTypeIPItem.Visibility = baseClass == typeof(DeviceWithIP) ? Visibility.Visible : Visibility.Collapsed;
        ConnectionTypeCOMItem.Visibility = baseClass == typeof(DeviceWithCOM) ? Visibility.Visible : Visibility.Collapsed;

        DeviceSettingsBorder.Visibility = Visibility.Visible;
      }
      catch (InvalidOperationException ex)
      {
        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    #endregion

    #region Обработчики выбора типа подключения и COM-порта.

    /// <summary>
    /// Обрабатывает изменение типа подключения, настраивая доступные параметры.
    /// При выборе IP скрывает COM-настройки и наоборот.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении выбора.</param>
    private void HandleConnectionTypeSelection(object sender, SelectionChangedEventArgs e)
    {
      if (ConnectionTypeSelectionBox.SelectedItem is ComboBoxItem selectedItem)
      {
        ConnectionSettingsBlock.Visibility = Visibility.Visible;
        AdditionalSettingsContainer.Visibility = Visibility.Visible;

        string selectedType = selectedItem.Content.ToString().ToLower();

        if (selectedType.Contains("ip"))
        {
          ShowIPSettings();
        }
        else
        {
          ShowCOMSettings();
        }
      }
    }

    /// <summary>
    /// Обрабатывает изменение выбранного COM-порта в выпадающем списке.
    /// Загружает настройки для выбранного порта, если он доступен.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleComPortSelection(object sender, SelectionChangedEventArgs e)
    {
      // if (ComPortComboBox.SelectedItem is string selectedPort)
      // {
      //   BaudRateSettings.Visibility = Visibility.Visible;
      //   FlowControlSettings.Visibility = Visibility.Visible;
      //   ParitySettings.Visibility = Visibility.Visible;
      //   StopBitsSettings.Visibility = Visibility.Visible;
      //   DataBitsSettings.Visibility = Visibility.Visible;
      //   VidSettings.Visibility = Visibility.Visible;
      //   PidSettings.Visibility = Visibility.Visible;
      // 
      //   (string vid, string pid) = GetVidPidFromComPort(selectedPort);
      //   VidData.Text = vid;
      //   PidData.Text = pid;
      // }
    }

    #endregion

    #region Обработчики параметров COM-подключения.

    /// <summary>
    /// Обрабатывает изменение выбранной скорости передачи данных (Baud Rate) в выпадающем списке.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleBaudRateSelection(object sender, SelectionChangedEventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        if (int.TryParse(selectedItem.Content.ToString(), out int baudRate))
        {
          BaudRate = baudRate;
        }
      }
    }

    /// <summary>
    /// Обрабатывает изменение количества бит данных (Data Bits) в выпадающем списке.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleDataBitsSelection(object sender, SelectionChangedEventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        if (byte.TryParse(selectedItem.Content.ToString(), out byte dataBits))
        {
          DataBits = dataBits;
        }
      }
    }

    /// <summary>
    /// Обрабатывает изменение режима управления потоком (Flow Control) в выпадающем списке.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleFlowControlSelection(object sender, SelectionChangedEventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        FlowControlMode = selectedItem.Content.ToString();
      }
    }

    /// <summary>
    /// Обрабатывает изменение количества стоповых бит (Stop Bits) в выпадающем списке.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleStopBitsSelection(object sender, SelectionChangedEventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        StopBitsMode = selectedItem.Content.ToString() switch
        {
          "1" => StopBits.One,
          "1.5" => StopBits.OnePointFive,
          "2" => StopBits.Two,
          _ => StopBits.None,
        };
      }
    }

    /// <summary>
    /// Обрабатывает изменение настройки четности (Parity) в выпадающем списке.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию о выборе.</param>
    private void HandleParitySelection(object sender, SelectionChangedEventArgs e)
    {
      if (ParitySelectionBox.SelectedItem is ComboBoxItem selectedItem)
      {
        ParityMode = selectedItem.Content.ToString() switch
        {
          "Чет" => Parity.Even,
          "Нечет" => Parity.Odd,
          "Нет" => Parity.None,
          "Маркер" => Parity.Mark,
          "Пробел" => Parity.Space,
          _ => Parity.None
        };
      }
    }

    #endregion

    #region Управление окном.

    /// <summary>
    /// Закрывает текущее окно при нажатии кнопки "Отмена".
    /// </summary>
    /// <param name="sender">Источник события (обычно кнопка).</param>
    /// <param name="e">Аргументы события, содержащие информацию о нажатии мыши.</param>
    private void CloseWindowOnClick(object sender, MouseButtonEventArgs e) => RequestClose?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Обрабатывает нажатие на кнопку "Сохранить", вызывая событие сохранения настроек.
    /// </summary>
    /// <param name="sender">Источник события (обычно кнопка).</param>
    /// <param name="e">Аргументы события, содержащие информацию о нажатии кнопки мыши.</param>
    private void HandleSaveClick(object sender, MouseButtonEventArgs e) => RequestSave?.Invoke(sender, e);

    #endregion

    #region Вспомогательные методы.

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

    /// <summary>
    /// Загружает список доступных COM-портов и заполняет выпадающий список.
    /// </summary>
    private void PopulateComPortList()
    {
      string[] ports = SerialPort.GetPortNames()
                                 .OrderBy(p => p)
                                 .ToArray();

      COMPortSelectionBox.ItemsSource = ports;
    }

    /// <summary>
    /// Настроить отображение блоков для подключения по IP.
    /// </summary>
    private void ShowIPSettings()
    {
      IPSettingsGrid.Visibility = Visibility.Visible;
      COMSettingsGrid.Visibility = Visibility.Collapsed;
      BaudRateGrid.Visibility = Visibility.Collapsed;
      FlowControlGrid.Visibility = Visibility.Collapsed;
      ParityGrid.Visibility = Visibility.Collapsed;
      StopBitsGrid.Visibility = Visibility.Collapsed;
      DataBitsGrid.Visibility = Visibility.Collapsed;

      if (!IsRackNumberEnabled)
      {
        IPAddressPart4Input.Text = DeviceNumberInput.Text;
      }
      else
      { 
        IPAddressPart4Input.Text = DeviceNumberInput.Text;
      }

      string number = RacksModelComboBox.SelectedItem.ToString();
      if (!string.IsNullOrEmpty(number))
      {
        IPAddressPart3Input.Text = number;
      }
      else
      {
        IPAddressPart3Input.Text = "0";
      }

    }

    /// <summary>
    /// Настроить отображение блоков для подключения по COM.
    /// </summary>
    private void ShowCOMSettings()
    {
      PopulateComPortList();
      IPSettingsGrid.Visibility = Visibility.Collapsed;
      COMSettingsGrid.Visibility = Visibility.Visible;
    }

    #endregion
  }
}
