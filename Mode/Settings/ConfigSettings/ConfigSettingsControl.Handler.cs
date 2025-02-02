using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Core.Abstract;
using Core.Enum;
using Core.Model;
using static AppConfig.FileLocations;

namespace Mode.Settings.ConfigSettings
{
  public partial class ConfigSettingsControl
  {
    /// <summary>
    /// Модель менеджера шасси.
    /// </summary>
    Core.ManagerShassy.Model managerShassyModel = null;

    /// <summary>
    /// Модель устройства коммутации шины.
    /// </summary>
    Core.DeviceBusCommutation.Model deviceBusCommutationModel = null;

    /// <summary>
    /// Модель модуля источника напряжения и тока.
    /// </summary>
    Core.ModuleVoltageCurrentSource.Model moduleVoltageCurrentSource = null;

    /// <summary>
    /// Список моделей модулей реле.
    /// </summary>
    readonly List<Core.ModuleRelayControl.Model> mkrModels = new List<Core.ModuleRelayControl.Model>();

    /// <summary>
    /// Модель быстрого измерителя.
    /// </summary>
    MeterBase fastMeterModel = null;

    /// <summary>
    /// Модель точного измерителя.
    /// </summary>
    MeterBase accurateMeterModel = null;

    /// <summary>
    /// Модель пробойной установки.
    /// </summary>
    BreakdownBase breakdownModel = null;

    /// <summary>
    /// Проверяет, является ли введенный текст числовым.
    /// </summary>
    /// <param name="e">Предоставляет данные для события PreviewTextInput.</param>
    private void CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!(int.TryParse(e.Text, out _)))
      {
        e.Handled = true;
      }
    }

    /// <summary>
    /// Создаёт новый элемент устройства для отображения в списке.
    /// </summary>
    /// <param name="parent">Родительский элемент интерфейса.</param>
    /// <param name="model">Модель устройства.</param>
    private void SetNewTreeViewItem(StackPanel parent, DeviceModel model)
    {
      Button button = new Button
      {
        Style = Application.Current.FindResource("CustomButtonStyle") as Style
      };

      var buttonBackgroundColor = (SolidColorBrush)Application.Current.Resources["SecondarySolidColorBrush"];
      button.Background = buttonBackgroundColor;
      button.Content = model.Name + " " + model.Number;


      if (model.DeviceType == DeviceEnum.Type.FastMeter)
      {
        button.Content = model.Name;
      }

      if (model.DeviceType == DeviceEnum.Type.AccurateMeter)
      {
        button.Content = model.Name;
      }

      if (model.DeviceType == DeviceEnum.Type.Breakdown)
      {
        button.Content = model.Name;
      }

      button.MouseEnter += (s, a) =>
      {
        button.Content = "Удалить?";
      };

      button.MouseLeave += (s, a) =>
      {

        if (model.DeviceType == DeviceEnum.Type.FastMeter)
        {
          button.Content = model.Name;
        }

        else if (model.DeviceType == DeviceEnum.Type.AccurateMeter)
        {
          button.Content = model.Name;
        }
        else if (model.DeviceType == DeviceEnum.Type.Breakdown)
        {
          button.Content = model.Name;
        }
        else
        {
          button.Content = model.Name + " " + model.Number;
        }
      };

      button.PreviewMouseLeftButtonDown += (s, a) =>
      {
        MessageBoxResult dialogResult = MessageBox.Show($"Удалить {model.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (dialogResult == MessageBoxResult.Yes)
        {
          parent.Children.Remove(button);
          switch (model.DeviceType)
          {
            case DeviceEnum.Type.DeviceBusCommutation:
              deviceBusCommutationModel = null;
              break;

            case DeviceEnum.Type.ModuleRelayControl:
              mkrModels.Remove(model as Core.ModuleRelayControl.Model);
              break;

            case DeviceEnum.Type.ManagerShassy:
              managerShassyModel = null;
              break;

            case DeviceEnum.Type.ModuleVoltageCurrentSource:
              moduleVoltageCurrentSource = null;
              break;

            case DeviceEnum.Type.FastMeter:
              fastMeterModel = null;
              break;


            case DeviceEnum.Type.Breakdown:
              breakdownModel = null;
              break;
          }

        }
      };
      parent.Children.Add(button);
    }

    /// <summary>
    /// Сохраняет текущую конфигурацию устройств.
    /// </summary>
    /// <exception cref="NotImplementedException">Выбрасывается, если метод не реализован.</exception>
    private void SaveConfig()
    {
      throw new NotImplementedException("Метод SaveConfig() ещё не реализован.");
    }


    /// <summary>
    /// Очищает отображение всех устройств в интерфейсе.
    /// </summary>
    private void ClearConfig()
    {
      managerShassyContent.Children.Clear();
      deviceBusCommutationContent.Children.Clear();
      moduleRelayContent.Children.Clear();
      moduleVoltageCurrentSourceContent.Children.Clear();
      accurateMeterContent.Children.Clear();
      breakdownContent.Children.Clear();
    }
  }
}
