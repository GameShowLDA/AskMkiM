using System.Windows;
using System.Windows.Controls;
using UI.Controls.GPT;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Объединяет низкоуровневые утилиты отправки команд и управления GPT.
  /// </summary>
  public partial class ServiceUtilitiesControl : UserControl
  {
    private SetCommand? setCommandControl;
    private GPTPunchControl? gptControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ServiceUtilitiesControl"/>.
    /// </summary>
    public ServiceUtilitiesControl()
    {
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

      UtilityContentPresenter.Content = gptControl ??= new GPTPunchControl();
      SideConsolePresenter.Content = setCommand;
      UtilityColumn.Width = new GridLength(1, GridUnitType.Star);
      UtilitySplitterColumn.Width = new GridLength(18);
      ConsoleColumn.Width = new GridLength(520);
      UtilitySplitter.Visibility = Visibility.Visible;
    }
  }
}
