using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Core.Abstract;
using Mode.Settings.ConfigSettings.CustomControls;
using static AppConfig.Config.SystemStateManager;
using static Mode.Settings.ConfigSettings.CustomControls.AccurateMeterConfigControl;
using static Mode.Settings.ConfigSettings.CustomControls.BreakdownControl;
using static Mode.Settings.ConfigSettings.CustomControls.DeviceBusCommutationConfigControl;
using static Mode.Settings.ConfigSettings.CustomControls.FastMeterConfigControl;
using static Mode.Settings.ConfigSettings.CustomControls.ManagerShassyConfigControl;
using static Mode.Settings.ConfigSettings.CustomControls.ModuleRelayConfigControl;
using static Mode.Settings.ConfigSettings.CustomControls.ModuleVoltageCurrentSourceConfigControl;

namespace Mode.Settings.ConfigSettings
{
  /// <summary>
  /// Частичный класс ConfigSettingsControl, содержащий логику обработки событий для различных элементов управления конфигурацией.
  /// Включает в себя методы для управления видимостью и поведением элементов интерфейса, обработки пользовательского ввода,
  /// и сохранения настроек для различных модулей системы, таких как менеджер шасси, модуль реле, устройство коммутации шины,
  /// модуль источника напряжения и тока, точный измеритель и быстрый измеритель.
  /// </summary>
  public partial class ConfigSettingsControl
  {
    /// <summary>
    /// Элемент управления конфигурацией менеджера шасси.
    /// </summary>
    ManagerShassyConfigControl managerShassyConfigControl;

    /// <summary>
    /// Элемент управления конфигурацией модуля реле.
    /// </summary>
    ModuleRelayConfigControl moduleRelayConfigControl;

    /// <summary>
    /// Элемент управления конфигурацией модуля источника напряжения и тока.
    /// </summary>
    ModuleVoltageCurrentSourceConfigControl moduleVoltageCurrentSourceConfigControl;

    /// <summary>
    /// Элемент управления конфигурацией устройства коммутации шины.
    /// </summary>
    DeviceBusCommutationConfigControl deviceBusCommutationConfigControl;

    /// <summary>
    /// Элемент управления конфигурацией точного измерителя.
    /// </summary>
    AccurateMeterConfigControl accurateMeterConfigControl;

    /// <summary>
    /// Элемент управления конфигурацией быстрого измерителя.
    /// </summary>
    FastMeterConfigControl fastMeterConfigControl;

    BreakdownControl breakdownControl;


    #region Менеджер шасси.

    /// <summary>
    /// Обрабатывает событие нажатия кнопки менеджера шасси.
    /// Переключает видимость содержимого менеджера шасси и изменяет толщину границы кнопки.
    /// </summary>
    private void ManagerShassyButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (managerShassyContent.Children.Count > 0)
      {
        if (managerShassyContent.Visibility == Visibility.Collapsed)
        {
          managerShassyContent.Visibility = Visibility.Visible;
          managerShassyButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          managerShassyContent.Visibility = Visibility.Collapsed;
          managerShassyButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления менеджера шасси.
    /// Создает новый элемент управления конфигурацией менеджера шасси и отображает его.
    /// </summary>
    private void AddManagerShassyButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      managerShassyConfigControl = new ManagerShassyConfigControl();
      managerShassyConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      managerShassyConfigControl.SaveButtonClicked += ManagerShassyConfigControl_SaveButtonClicked;
      contentPanel.Child = managerShassyConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации менеджера шасси.
    /// Устанавливает модель менеджера шасси и скрывает элемент управления конфигурацией.
    /// </summary>
    private void ManagerShassyConfigControl_SaveButtonClicked(object sender, ManagerShassyModelModelEventArgs e)
    {
      Core.ManagerShassy.Model model = e.Model;
      SetManagerShassy(model);
      managerShassyConfigControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }

    #endregion

    #region МКР.

    /// <summary>
    /// Обрабатывает событие нажатия кнопки модуля реле.
    /// Переключает видимость содержимого модуля реле и изменяет толщину границы кнопки.
    /// </summary>
    private void ModuleRelayButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (moduleRelayContent.Children.Count > 0)
      {
        if (moduleRelayContent.Visibility == Visibility.Collapsed)
        {
          moduleRelayContent.Visibility = Visibility.Visible;
          moduleRelayButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          moduleRelayContent.Visibility = Visibility.Collapsed;
          moduleRelayButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления модуля реле.
    /// Создает новый элемент управления конфигурацией модуля реле и отображает его.
    /// </summary>
    private void AddModuleRelayButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      moduleRelayConfigControl = new ModuleRelayConfigControl();
      moduleRelayConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      moduleRelayConfigControl.SaveButtonClicked += ModuleRelayConfigControl_SaveButtonClicked;
      contentPanel.Child = moduleRelayConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации модуля реле.
    /// Добавляет модель модуля реле и скрывает элемент управления конфигурацией при успешном добавлении.
    /// </summary>
    private void ModuleRelayConfigControl_SaveButtonClicked(object sender, ModuleRelayControlModelEventArgs e)
    {
      Core.ModuleRelayControl.Model model = e.Model;
      if (AddMkr(model))
      {
        moduleRelayConfigControl.Visibility = Visibility.Collapsed;
        contentPanel.Child = null;
      }
    }

    #endregion

    #region УКШ.

    /// <summary>
    /// Обрабатывает событие нажатия кнопки устройства коммутации шины.
    /// Переключает видимость содержимого устройства коммутации шины и изменяет толщину границы кнопки.
    /// </summary>
    private void DeviceBusCommutationButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (deviceBusCommutationContent.Children.Count > 0)
      {
        if (deviceBusCommutationContent.Visibility == Visibility.Collapsed)
        {
          deviceBusCommutationContent.Visibility = Visibility.Visible;
          deviceBusCommutationButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          deviceBusCommutationContent.Visibility = Visibility.Collapsed;
          deviceBusCommutationButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления устройства коммутации шины.
    /// Создает новый элемент управления конфигурацией устройства коммутации шины и отображает его.
    /// </summary>
    private void AddDeviceBusCommutationButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      deviceBusCommutationConfigControl = new DeviceBusCommutationConfigControl();
      deviceBusCommutationConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      deviceBusCommutationConfigControl.SaveButtonClicked += DeviceBusCommutationConfigControl_SaveButtonClicked;
      contentPanel.Child = deviceBusCommutationConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации устройства коммутации шины.
    /// Устанавливает модель устройства коммутации шины и скрывает элемент управления конфигурацией.
    /// </summary>
    private void DeviceBusCommutationConfigControl_SaveButtonClicked(object sender, DeviceBusCommutationModelEventArgs e)
    {
      Core.DeviceBusCommutation.Model model = e.Model;
      SetDeviceBusCommutation(model);
      deviceBusCommutationConfigControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }

    #endregion

    #region МИНТ.

    /// <summary>
    /// Обрабатывает событие нажатия кнопки модуля источника напряжения и тока.
    /// Переключает видимость содержимого модуля источника напряжения и тока и изменяет толщину границы кнопки.
    /// </summary>
    private void ModuleVoltageCurrentSourceButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (moduleVoltageCurrentSourceContent.Children.Count > 0)
      {
        if (moduleVoltageCurrentSourceContent.Visibility == Visibility.Collapsed)
        {
          moduleVoltageCurrentSourceContent.Visibility = Visibility.Visible;
          moduleVoltageCurrentSourceButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          moduleVoltageCurrentSourceContent.Visibility = Visibility.Collapsed;
          moduleVoltageCurrentSourceButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления модуля источника напряжения и тока.
    /// Создает новый элемент управления конфигурацией модуля источника напряжения и тока и отображает его.
    /// </summary>
    private void AddModuleVoltageCurrentSourceButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      moduleVoltageCurrentSourceConfigControl = new ModuleVoltageCurrentSourceConfigControl();
      moduleVoltageCurrentSourceConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      moduleVoltageCurrentSourceConfigControl.SaveButtonClicked += ModuleVoltageCurrentSourceConfigControl_SaveButtonClicked;
      contentPanel.Child = moduleVoltageCurrentSourceConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации модуля источника напряжения и тока.
    /// Устанавливает модель модуля источника напряжения и тока и скрывает элемент управления конфигурацией.
    /// </summary>
    private void ModuleVoltageCurrentSourceConfigControl_SaveButtonClicked(object sender, ModuleVoltageCurrentSourceModelEventArgs e)
    {
      Core.ModuleVoltageCurrentSource.Model model = e.Model;
      SetMint(model);
      moduleVoltageCurrentSourceConfigControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }

    #endregion

    #region Измеритель точный.

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации точного измерителя.
    /// Устанавливает модель точного измерителя и скрывает элемент управления конфигурацией.
    /// </summary>
    private void AccurateMeterConfigControl_SaveButtonClicked(object sender, AccurateMeterModelEventArgs e)
    {
      MeterBase model = e.Model;
      SetAccurateMeter(model);
      accurateMeterConfigControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления точного измерителя.
    /// Создает новый элемент управления конфигурацией точного измерителя и отображает его.
    /// </summary>
    private void AddAccurateMeterButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      accurateMeterConfigControl = new AccurateMeterConfigControl();
      accurateMeterConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      accurateMeterConfigControl.SaveButtonClicked += AccurateMeterConfigControl_SaveButtonClicked;
      contentPanel.Child = accurateMeterConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки точного измерителя.
    /// Переключает видимость содержимого точного измерителя и изменяет толщину границы кнопки.
    /// </summary>
    private void AccurateMeterButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (accurateMeterContent.Children.Count > 0)
      {
        if (accurateMeterContent.Visibility == Visibility.Collapsed)
        {
          accurateMeterContent.Visibility = Visibility.Visible;
          accurateMeterButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          accurateMeterContent.Visibility = Visibility.Collapsed;
          accurateMeterButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }

    #endregion

    #region Измерительный быстрый.

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации быстрого измерителя.
    /// Устанавливает модель быстрого измерителя и скрывает элемент управления конфигурацией.
    /// </summary>
    private void FastMeterConfigControl_SaveButtonClicked(object sender, FastMeterModelEventArgs e)
    {
      MeterBase model = e.Model;
      SetFastMeter(model);
      fastMeterConfigControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки добавления быстрого измерителя.
    /// Создает новый элемент управления конфигурацией быстрого измерителя и отображает его.
    /// </summary>
    private void AddFastMeterButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      fastMeterConfigControl = new FastMeterConfigControl();
      fastMeterConfigControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      fastMeterConfigControl.SaveButtonClicked += FastMeterConfigControl_SaveButtonClicked;
      contentPanel.Child = fastMeterConfigControl;
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки быстрого измерителя.
    /// Переключает видимость содержимого быстрого измерителя и изменяет толщину границы кнопки.
    /// </summary>
    private void FastMeterButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (breakdownContent.Children.Count > 0)
      {
        if (breakdownContent.Visibility == Visibility.Collapsed)
        {
          breakdownContent.Visibility = Visibility.Visible;
          fastMeterButton.BorderThickness = new Thickness(3, 3, 3, 0);
        }
        else
        {
          breakdownContent.Visibility = Visibility.Collapsed;
          fastMeterButton.BorderThickness = new Thickness(0, 0, 0, 3);
        }
      }
    }
    #endregion

    #region Пробойная установка.

    /// <summary>
    /// Обрабатывает событие сохранения конфигурации быстрого измерителя.
    /// Устанавливает модель быстрого измерителя и скрывает элемент управления конфигурацией.
    /// </summary>
    private void BreakdownControl_SaveButtonClicked(object sender, BreakdownModelEventArgs e)
    {
      BreakdownBase model = e.Model;
      SetBreakdown(model);
      breakdownControl.Visibility = Visibility.Collapsed;
      contentPanel.Child = null;
    }
    private void AddBreakdownButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      breakdownControl = new BreakdownControl();
      breakdownControl.CancelButtonClicked += (s, a) => contentPanel.Child = null;
      breakdownControl.SaveButtonClicked += BreakdownControl_SaveButtonClicked;
      contentPanel.Child = breakdownControl;
    }

    #endregion

    /// <summary>
    /// Обрабатывает событие PreviewTextInput для TextBox countPoints.
    /// Это событие возникает прямо перед началом текстового компонования и может использоваться для отмены компоновки.
    /// </summary>
    /// <param name="sender">Объект, к которому прикреплен обработчик событий.</param>
    /// <param name="e">Предоставляет данные для события PreviewTextInput.</param>
    private void CountPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      CheckIsNumeric(e);
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки автоматической конфигурации.
    /// Запускает процесс анализа устройств.
    /// </summary>
    private async void AutoConfig_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (await GetIsActivePower())
      {
        ParseDevices();
      }
      else
      {
        MessageBox.Show("Пожалуйста, подключитесь к системе для автоматической конфигурации оборудования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия кнопки сохранения конфигурации.
    /// Сохраняет текущую конфигурацию и отображает сообщение об успешном сохранении.
    /// </summary>
    private void SaveConfiguration_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      SaveConfig();
      MessageBox.Show("Данные успешно сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Обрабатывает событие наведения курсора на кнопку "плюс".
    /// Изменяет цвет переднего плана кнопки на активный.
    /// </summary>
    private void PlusButton_MouseEnter(object sender, MouseEventArgs e)
    {
      var buttonForegroundColor = (SolidColorBrush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      (sender as UI.Components.PlusButtonControl).Foreground = buttonForegroundColor;
    }

    /// <summary>
    /// Обрабатывает событие ухода курсора с кнопки "плюс".
    /// Возвращает цвет переднего плана кнопки к исходному.
    /// </summary>
    private void PlusButton_MouseLeave(object sender, MouseEventArgs e)
    {
      var buttonForegroundColor = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
      (sender as UI.Components.PlusButtonControl).Foreground = buttonForegroundColor;
    }
  }
}
