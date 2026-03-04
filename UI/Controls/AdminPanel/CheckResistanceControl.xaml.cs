using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using DataBaseConfiguration.Services.Device;
using System;
using System.Collections.Generic;
using System.Linq;
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
using UI.Components;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Логика взаимодействия для CheckResistanceControl.xaml
  /// </summary>
  public partial class CheckResistanceControl : UserControl
  {
    Dictionary<IRelaySwitchModule, double> defaultData = new();
    Dictionary<IRelaySwitchModule, double> newData = new();
    public Action SetDefaultValue;
    public CheckResistanceControl()
    {
      InitializeComponent();
      Loaded += CheckResistanceControl_Loaded;
      VisibleButton(Visibility.Collapsed);
    }

    private void CheckResistanceControl_Loaded(object sender, RoutedEventArgs e)
    {
      var rms = new RelaySwitchModuleServices().GetAll();

      foreach (var r in rms)
      {
        var control = new SettingsResistanceInput(r, this);
        DevicesControl.Children.Add(control);
        defaultData.Add(r, r.SwitchResistance);
        newData.Add(r, r.SwitchResistance);
        control.ChangeTextEvent += (value) =>
        {
          if (defaultData[r] != value)
          {
            VisibleButton(Visibility.Visible);
            newData[r] = value;
          }
        };
      }

      Success.PreviewMouseDown += Success_PreviewMouseDown;
      Error.PreviewMouseDown += Error_PreviewMouseDown;
    }

    private void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      foreach (var pair in newData)
      {
        var module = pair.Key;
        var value = pair.Value;
        module.SwitchResistance = value;

        new RelaySwitchModuleServices().UpdateResistance(module.NumberChassis, module.Number, value);
      }

      defaultData = new Dictionary<IRelaySwitchModule, double>(newData);

      VisibleButton(Visibility.Collapsed);
    }

    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      SetDefaultValue?.Invoke();
      VisibleButton(Visibility.Collapsed);
    }

    private void VisibleButton(Visibility visible)
    {
      Success.Visibility = visible;
      Error.Visibility = visible;
    }
  }
}
