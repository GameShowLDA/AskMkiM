using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для ReplaseWordButton.xaml.
  /// </summary>
  public partial class ReplaseWordButton : UserControl
  {
    /// <summary>
    /// Конструктор для создания экземпляра кнопки "Заменить слово".
    /// Инициализирует компоненты кнопки.
    /// </summary>
    public ReplaseWordButton()
    {
      InitializeComponent();
    }

    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
      replaceWord.Opacity = 1.0;
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
      replaceWord.Opacity = 0.7;
    }
  }
}
