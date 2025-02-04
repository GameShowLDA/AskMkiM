using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using AppConfig.DataBase.Models;
using Core.ConfigCollector;
using Core.Model;
using static Utilities.LoggerUtility;
using NewCore.Base;
using AppConfig.DataBase.Services;
using NewCore.Device;
using System.Net;
using Core.Abstract;
using NewCore.Interface;

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
              Console.WriteLine($"Это быстрый измеритель: {fastMeter.Name}");
              break;

            case PowerSourceModuleEntity powerSource:
              Console.WriteLine($"Это источник питания: {powerSource.Name}");
              break;

            case PrecisionMeterEntity precisionMeter:
              Console.WriteLine($"Это точный измеритель: {precisionMeter.Name}");
              break;

            case RelaySwitchModuleEntity relaySwitch:
              Console.WriteLine($"Это релейный коммутатор: {relaySwitch.Name}");
              break;

            case SwitchingDeviceEntity switchingDevice:
              Console.WriteLine($"Это устройство коммутации: {switchingDevice.Name}");
              break;

            case ChassisManagerEntity chassisManager:
              Console.WriteLine($"Это менеджер шасси: {chassisManager.Name}");
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
