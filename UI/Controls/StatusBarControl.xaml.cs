using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для StatusBarControl.xaml
  /// </summary>
  public partial class StatusBarControl : UserControl
  {
    public StatusBarControl()
    {
      InitializeComponent();
    }

    public interface ICommandProvider
    {
      ICommand ChangeEncodingCommand { get; }
      ICommand ToggleEncodingCommand { get; }
    }

    /// <summary>
    /// Обработчик нажатия правой кнопкой мыши по полю с кодировкой. При нажатии открывает весь список кодировок для выбора.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EncodingTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ICommandProvider provider && provider.ChangeEncodingCommand.CanExecute(e))
      {
        provider.ChangeEncodingCommand.Execute(e);
      }
    }

    /// <summary>
    /// Обработчик нажатия левой кнопкой мыши по полю с кодировкой. При нажатии меняет между собой кодировки UTF-8 и DOS.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EncodingTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ICommandProvider provider && provider.ChangeEncodingCommand.CanExecute(e))
      {
        provider.ToggleEncodingCommand.Execute(e);
      }
    }

  }
}
