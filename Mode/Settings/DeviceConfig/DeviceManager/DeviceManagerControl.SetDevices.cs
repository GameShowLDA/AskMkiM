using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  public partial class DeviceManagerControl
  {
    public void AddDevice<T>(T device) where T : IDevice
    {
      var type = device.GetType();
      switch (device)
      {
        case BreakdownTesterEntity breakdownTester:
          SetNewTreeViewItem(breakdownContent, breakdownTester);
          break;

        case FastMeterEntity fastMeter:
          SetNewTreeViewItem(fastMeterContent, fastMeter);
          break;

        case PowerSourceModuleEntity powerSource:
          SetNewTreeViewItem(moduleVoltageCurrentSourceContent, powerSource);
          break;

        case PrecisionMeterEntity precisionMeter:
          SetNewTreeViewItem(accurateMeterContent, precisionMeter);
          break;

        case RelaySwitchModuleEntity relaySwitch:
          SetNewTreeViewItem(moduleRelayContent, relaySwitch);
          break;

        case SwitchingDeviceEntity switchingDevice:
          SetNewTreeViewItem(deviceBusCommutationContent, switchingDevice);
          break;

        default:
          Console.WriteLine("Неизвестный тип устройства.");
          break;
      }
    }

    /// <summary>
    /// Создаёт новый элемент устройства для отображения в списке.
    /// </summary>
    /// <param name="parent">Родительский элемент интерфейса.</param>
    /// <param name="model">Модель устройства.</param>
    private void SetNewTreeViewItem<T>(StackPanel parent, T model) where T : IDevice
    {
      Button button = new Button
      {
        Style = Application.Current.FindResource("CustomButtonStyle") as Style
      };

      var buttonBackgroundColor = (SolidColorBrush)Application.Current.Resources["SecondarySolidColorBrush"];
      button.Background = buttonBackgroundColor;
      button.Content = model.Name + " " + model.Number;
      button.MouseEnter += (s, a) =>
      {
        button.Content = "Удалить?";
      };

      button.MouseLeave += (s, a) =>
      {
        button.Content = model.Name + " " + model.Number;
      };

      button.PreviewMouseLeftButtonDown += (s, a) =>
      {
        MessageBoxResult dialogResult = MessageBox.Show($"Удалить {model.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (dialogResult == MessageBoxResult.Yes)
        {
          parent.Children.Remove(button);
          switch (model)
          {
            case BreakdownTesterEntity breakdownTester:
              new BreakdownTesterRepository(AppConfig.Config.SystemStateManager.Context).Delete(breakdownTester.Id);
              break;

            case FastMeterEntity fastMeter:
              new FastMeterRepository(AppConfig.Config.SystemStateManager.Context).Delete(fastMeter.Id);
              break;

            case PowerSourceModuleEntity powerSource:
              new PowerSourceModuleRepository(AppConfig.Config.SystemStateManager.Context).Delete(powerSource.Id);
              break;

            case PrecisionMeterEntity precisionMeter:
              new PrecisionMeterRepository(AppConfig.Config.SystemStateManager.Context).Delete(precisionMeter.Id);
              break;

            case RelaySwitchModuleEntity relaySwitch:
              new RelaySwitchModuleRepository(AppConfig.Config.SystemStateManager.Context).Delete(relaySwitch.Id);
              break;

            case SwitchingDeviceEntity switchingDevice:
              new SwitchingDeviceRepository(AppConfig.Config.SystemStateManager.Context).Delete(switchingDevice.Id);
              break;

            case ChassisManagerEntity chassisManager:
              new ChassisManagerRepository(AppConfig.Config.SystemStateManager.Context).Delete(chassisManager.Id);
              break;

            default:
              Console.WriteLine("Неизвестный тип устройства.");
              break;
          }
        }
      };

      parent.Children.Add(button);
    }
  }
}
