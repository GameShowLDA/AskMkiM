using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Interface;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace Mode.Settings.DeviceConfig.BaseSettings
{
  partial class BaseSettingsControl
  {
    private void exit_PreviewMouseDown(object sender, MouseButtonEventArgs e) => RequestClose?.Invoke(this, EventArgs.Empty);
    private void IpPart_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    private void save_PreviewMouseDown(object sender, MouseButtonEventArgs e) => SaveDevice();
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
          ComSettings.Visibility = Visibility.Collapsed;
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
          LoadComPorts();
          IpSettings.Visibility = Visibility.Collapsed;
          ComSettings.Visibility = Visibility.Visible;
        }
      }
    }
    private void DeviceModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (deviceModelComboBox.SelectedItem is string selectedModel &&
          deviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        try
        {
          Type baseClass = DetermineBaseClass(selectedType);

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

          DefaultSettingDevice.Visibility = Visibility.Visible;
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
