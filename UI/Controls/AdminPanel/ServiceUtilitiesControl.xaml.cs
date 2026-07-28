using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.UI.Features.ServiceTools.Gpt;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Объединяет низкоуровневые утилиты отправки команд и управления GPT.
  /// </summary>
  public partial class ServiceUtilitiesControl : UserControl
  {
    private SetCommand? setCommandControl;
    private GPTPunchControl? gptControl;
    private readonly Func<Task<IBreakdownTester?>> gptProvider;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ServiceUtilitiesControl"/>.
    /// </summary>
    /// <param name="gptProvider">Функция получения настроенной пробойной установки.</param>
    public ServiceUtilitiesControl(Func<Task<IBreakdownTester?>> gptProvider)
    {
      this.gptProvider = gptProvider
        ?? throw new ArgumentNullException(nameof(gptProvider));
      InitializeComponent();
      SetCommandTab.IsChecked = true;
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
  }
}
