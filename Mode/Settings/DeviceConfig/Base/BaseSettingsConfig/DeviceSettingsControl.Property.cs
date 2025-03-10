using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Interface;
using System.Windows;

namespace Mode.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  partial class DeviceSettingsControl
  {
    private IHeadUnit _headUnit;
    public Dictionary<string, Type> DeviceModelMap = new Dictionary<string, Type>();

    public EventHandler SaveEvent;

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
  }
}
