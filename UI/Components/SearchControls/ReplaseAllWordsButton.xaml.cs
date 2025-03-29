using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для ReplaseAllWordsButton.xaml.
  /// </summary>
  public partial class ReplaseAllWordsButton : UserControl
  {
    /// <summary>
    /// Конструктор для создания экземпляра кнопки "Заменить все слова".
    /// Инициализирует компоненты кнопки.
    /// </summary>
    public ReplaseAllWordsButton()
    {
      InitializeComponent();
    }

    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
      replaceAllWords.Opacity = 1.0;
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
      replaceAllWords.Opacity = 0.7;
    }
  }
}
