using System.IO.Ports;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.Base
{
  /// <summary>
  /// Класс, управляющий событиями и логикой конфигурации устройств.
  /// Работает с устройствами, реализующими интерфейс IDevice.
  /// </summary>
  /// <typeparam name="T">Тип устройства, реализующего интерфейс IDevice.</typeparam>
  internal class BaseEvent<T> where T : class, IDevice
  {

    /// <summary>
    /// Событие для уведомления о закрытии окна или формы.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Словарь, отображающий названия моделей устройств и их типы.
    /// </summary>
    internal Dictionary<string, Type> DeviceModelMap = new Dictionary<string, Type>();

    /// <summary>
    /// Обработчик события нажатия кнопки выхода. Инициирует закрытие окна.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события нажатия кнопки мыши.</param>
    internal void ExitPreviewMouseDown(object sender, MouseButtonEventArgs e) => RequestClose?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Загружает доступные модели устройств и отображает их в ComboBox.
    /// </summary>
    internal void LoadDeviceModels(ComboBox DeviceModelComboBox)
    {
      var models = ReflectionHelper.GetAllImplementations<T>();

      DeviceModelMap = models
          .Select(t => Activator.CreateInstance(t) as T)
          .Where(instance => instance != null)
          .ToDictionary(instance => instance.Name, instance => instance.GetType());

      DeviceModelComboBox.ItemsSource = DeviceModelMap.Keys; // Отображаем только имена в ComboBox
    }

    /// <summary>
    /// Ограничивает ввод только числовыми значениями для частей IP-адреса.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события ввода текста.</param>
    internal void IpPartPreviewTextInput(TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    /// <summary>
    /// Проверяет корректность введённых данных для частей IP-адреса.
    /// Ограничивает значение диапазоном от 0 до 255.
    /// </summary>
    /// <param name="sender">Источник события (TextBox).</param>
    /// <param name="e">Данные события изменения текста.</param>
    internal void IpPartTextChanged(object sender)
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

    /// <summary>
    /// Обрабатывает изменение выбранной модели устройства и отображает
    /// соответствующие элементы управления в зависимости от типа устройства (IP или COM).
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события изменения выбора.</param>
    internal void DeviceModelComboBox_SelectionChanged(ComboBox DeviceModelComboBox, ComboBoxItem ComItem, ComboBoxItem IpItem, Grid DefaultSettingDevice)
    {
      if (DeviceModelComboBox.SelectedItem is string selectedModel &&
          DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        try
        {
          Type baseClass = DetermineBaseClass(selectedType);

          if (baseClass == typeof(DeviceWithIP))
          {
            ComItem.Visibility = Visibility.Collapsed;
            IpItem.Visibility = Visibility.Visible;
          }
          else if (baseClass == typeof(DeviceWithCOM))
          {
            ComItem.Visibility = Visibility.Visible;
            IpItem.Visibility = Visibility.Collapsed;
          }

          DefaultSettingDevice.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
          MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    /// <summary>
    /// Определяет базовый класс выбранного типа устройства (DeviceWithIP или DeviceWithCOM).
    /// Выбрасывает исключение, если устройство наследует оба класса или ни один.
    /// </summary>
    /// <param name="selectedType">Тип выбранного устройства.</param>
    /// <returns>Тип базового класса устройства.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если устройство наследует одновременно DeviceWithIP и DeviceWithCOM или ни один из них.</exception>
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

    /// <summary>
    /// Ограничивает ввод значений в TextBox до диапазона от 0 до 250.
    /// </summary>
    /// <param name="sender">Источник события (TextBox).</param>
    /// <param name="e">Данные события изменения текста.</param>
    internal void TextBox_TextChanged(object sender)
    {
      if (sender is TextBox textBox)
      {
        if (int.TryParse(textBox.Text, out int value))
        {
          if (value > 250)
          {
            textBox.Text = "250";
            textBox.CaretIndex = textBox.Text.Length;
          }
        }
      }
    }

    /// <summary>
    /// Проверяет вставляемый текст на соответствие числовому формату.
    /// Отменяет вставку, если вставляемый текст содержит нечисловые символы.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события вставки объекта.</param>
    internal void TextBox_Pasting(DataObjectPastingEventArgs e)
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
