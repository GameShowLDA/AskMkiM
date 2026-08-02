using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.UI.Features.ServiceTools.Chassis;
using Ask.UI.Features.ServiceTools.Gpt;
using Ask.UI.Features.ServiceTools.Multimeter;
using Ask.UI.Features.ServiceTools.RelaySwitchModule;
using Ask.UI.Features.ServiceTools.SwitchingDevice;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Объединяет низкоуровневые утилиты отправки команд и управления GPT.
  /// </summary>
  public partial class ServiceUtilitiesControl : UserControl
  {
    private readonly Func<Task<IBreakdownTester?>> gptProvider;
    private readonly Func<Task<ISwitchingDevice?>> switchingDeviceProvider;
    private readonly Func<Task<IReadOnlyList<IRelaySwitchModule>>> relaySwitchModulesProvider;
    private readonly Func<Task<IReadOnlyList<IMultimeter>>> multimetersProvider;
    private readonly Func<Task<IChassisManager?>> chassisProvider;
    private SetCommand? setCommandControl;
    private GPTPunchControl? gptControl;
    private SwitchingDeviceControl? switchingDeviceControl;
    private RelaySwitchModuleControl? relaySwitchModuleControl;
    private MultimeterControl? multimeterControl;
    private ChassisControl? chassisControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ServiceUtilitiesControl"/>.
    /// </summary>
    /// <param name="gptProvider">Функция получения настроенной пробойной установки.</param>
    /// <param name="switchingDeviceProvider">Функция получения настроенного устройства коммутации шин.</param>
    /// <param name="relaySwitchModulesProvider">Функция получения модулей коммутации реле.</param>
    /// <param name="multimetersProvider">Функция получения мультиметров.</param>
    /// <param name="chassisProvider">Функция получения контроллера шасси.</param>
    public ServiceUtilitiesControl(
      Func<Task<IBreakdownTester?>> gptProvider,
      Func<Task<ISwitchingDevice?>> switchingDeviceProvider,
      Func<Task<IReadOnlyList<IRelaySwitchModule>>> relaySwitchModulesProvider,
      Func<Task<IReadOnlyList<IMultimeter>>> multimetersProvider,
      Func<Task<IChassisManager?>> chassisProvider)
    {
      this.gptProvider = gptProvider
        ?? throw new ArgumentNullException(nameof(gptProvider));
      this.switchingDeviceProvider = switchingDeviceProvider
        ?? throw new ArgumentNullException(nameof(switchingDeviceProvider));
      this.relaySwitchModulesProvider = relaySwitchModulesProvider
        ?? throw new ArgumentNullException(nameof(relaySwitchModulesProvider));
      this.multimetersProvider = multimetersProvider
        ?? throw new ArgumentNullException(nameof(multimetersProvider));
      this.chassisProvider = chassisProvider
        ?? throw new ArgumentNullException(nameof(chassisProvider));
      InitializeComponent();
      SetCommandTab.IsChecked = true;
    }

    private void ChassisTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      UtilityContentPresenter.Content = null;
      SideConsolePresenter.Content = null;
      UtilityContentPresenter.Content = chassisControl ??= new ChassisControl(chassisProvider);
      SideConsolePresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
      UtilityColumn.Width = GridLength.Auto;
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitter.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Отображает утилиту отправки произвольных команд.
    /// </summary>
    /// <param name="sender">Выбранная вкладка.</param>
    /// <param name="e">Данные события выбора.</param>
    private void SetCommandTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      SideConsolePresenter.Content = null;
      UtilityContentPresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Stretch;
      UtilityColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitterColumn.Width = new GridLength(0);
      ConsoleColumn.Width = new GridLength(0);
      UtilitySplitter.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Отображает панель управления пробойной установкой GPT.
    /// </summary>
    /// <param name="sender">Выбранная вкладка.</param>
    /// <param name="e">Данные события выбора.</param>
    private void GptTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      UtilityContentPresenter.Content = null;
      SideConsolePresenter.Content = null;

      UtilityContentPresenter.Content = gptControl ??= new GPTPunchControl(gptProvider);
      SideConsolePresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
      UtilityColumn.Width = GridLength.Auto;
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitter.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Отображает панель управления устройством коммутации шин.
    /// </summary>
    /// <param name="sender">Выбранная вкладка.</param>
    /// <param name="e">Данные события выбора.</param>
    private void SwitchingDeviceTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      UtilityContentPresenter.Content = null;
      SideConsolePresenter.Content = null;

      UtilityContentPresenter.Content = switchingDeviceControl
        ??= new SwitchingDeviceControl(switchingDeviceProvider);
      SideConsolePresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
      UtilityColumn.Width = GridLength.Auto;
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitter.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Отображает панель управления модулями коммутации реле.
    /// </summary>
    /// <param name="sender">Выбранная вкладка.</param>
    /// <param name="e">Данные события выбора.</param>
    private void RelaySwitchModuleTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      UtilityContentPresenter.Content = null;
      SideConsolePresenter.Content = null;
      UtilityContentPresenter.Content = relaySwitchModuleControl
        ??= new RelaySwitchModuleControl(relaySwitchModulesProvider);
      SideConsolePresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
      UtilityColumn.Width = GridLength.Auto;
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitter.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Отображает панель сервисного управления мультиметрами.
    /// </summary>
    /// <param name="sender">Выбранная вкладка.</param>
    /// <param name="e">Данные события выбора.</param>
    private void MultimeterTab_Checked(object sender, RoutedEventArgs e)
    {
      var setCommand = setCommandControl ??= new SetCommand();

      UtilityContentPresenter.Content = null;
      SideConsolePresenter.Content = null;
      UtilityContentPresenter.Content = multimeterControl
        ??= new MultimeterControl(multimetersProvider);
      SideConsolePresenter.Content = setCommand;
      UtilityContentPresenter.HorizontalAlignment = HorizontalAlignment.Left;
      UtilityColumn.Width = GridLength.Auto;
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitter.Visibility = Visibility.Visible;
    }
  }
}
