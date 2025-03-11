using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Interface;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using NewCore.Base;

namespace Mode.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  partial class DeviceSettingsControl
  {
    private IHeadUnit _headUnit;
    public Dictionary<string, Type> DeviceModelMap = new Dictionary<string, Type>();

    public EventHandler SaveEvent;
    public EventHandler RequestClose;

    public string NameDevice
    {
      get
      {
        return Header.Text;
      }

      set
      {
        Header.Text = $"Настройка устройства: \"{value}\"";
      }
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
        DependencyProperty.Register("AdditionalSettings", typeof(UIElement), typeof(DeviceSettingsControl), new PropertyMetadata(null, OnAdditionalSettingsChanged));

    /// <summary>
    /// Получает значение первой части IP-адреса из TextBox.
    /// Возвращает -1, если введенные данные не являются числом.
    /// </summary>
    public int IpPart1Value
    {
      get
      {
        if (int.TryParse(IpPart1.Text, out int ipValue))
        {
          return ipValue;
        }
        return -1;
      }
    }

    /// <summary>
    /// Получает значение второй части IP-адреса из TextBox.
    /// Возвращает -1, если введенные данные не являются числом.
    /// </summary>
    public int IpPart2Value
    {
      get
      {
        if (int.TryParse(IpPart2.Text, out int ipValue))
        {
          return ipValue;
        }
        return -1;
      }
    }

    /// <summary>
    /// Получает значение третьей части IP-адреса из TextBox.
    /// Возвращает -1, если введенные данные не являются числом.
    /// </summary>
    public int IpPart3Value
    {
      get
      {
        if (int.TryParse(IpPart3.Text, out int ipValue))
        {
          return ipValue;
        }
        return -1;
      }
    }

    /// <summary>
    /// Получает значение четвертой части IP-адреса из TextBox.
    /// Возвращает -1, если введенные данные не являются числом.
    /// </summary>
    public int IpPart4Value
    {
      get
      {
        if (int.TryParse(IpPart4.Text, out int ipValue))
        {
          return ipValue;
        }
        return -1;
      }
    }


    /// <summary>
    /// Получает значение скорости передачи данных (BaudRate) из выбранного элемента ComboBox.
    /// Возвращает -1, если элемент не выбран, отсутствует или не может быть преобразован в число.
    /// </summary>
    public int BaudRateValue
    {
      get
      {
        if (BaudRateSelectionBox?.SelectedItem is ComboBoxItem selectedItem)
        {
          if (int.TryParse(selectedItem.Content?.ToString(), out int baudRate))
          {
            return baudRate;
          }
        }
        return -1;
      }
    }

    public int DataBitsValue
    {
      get
      {
        if (DataBitsSelectionBox?.SelectedItem is ComboBoxItem selectedItem)
        {
          if (int.TryParse(selectedItem.Content?.ToString(), out int baudRate))
          {
            return baudRate;
          }
        }
        return -1;
      }
    }

    public Parity ParityValue
    {
      get
      {
        BaseHandler<IDevice>.ValuePairs.TryGetValue(ParitySelectionBox.SelectedItem.ToString(), out Parity parity);
        return parity;
      }
    }

    public StopBits StopBitsValue
    {
      get
      {
        BaseHandler<IDevice>.StopBitsPairs.TryGetValue(StopBitsSelectionBox.SelectedItem.ToString(), out StopBits stopBit);
        return stopBit;
      }
    }

    public string PortName => COMPortSelectionBox.Text;

    public int NumberDevice
    {
      get
      {

        if (int.TryParse(DeviceNumberTextBox.Text, out int number))
        {
          return number;
        }

        return -1;
      }
    }
  }
}
