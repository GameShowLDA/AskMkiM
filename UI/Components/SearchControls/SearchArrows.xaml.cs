using Ask.Core.Services.EventCore.Adapters;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Выбирает действие поиска и запускает его нажатием на значок.
  /// </summary>
  public partial class SearchArrows : UserControl
  {
    /// <summary>
    /// Создаёт переключатель действий поиска.
    /// </summary>
    public SearchArrows()
    {
      InitializeComponent();
      InitializeArrows();
    }

    /// <summary>
    /// Заполняет список действий с подписями и сочетаниями клавиш.
    /// </summary>
    private void InitializeArrows()
    {
      searchArrowsComboBox.ItemsSource = new List<ArrowItem>
      {
        new ArrowItem { Name = "FindNext", Caption = "Найти далее", Gesture = "F3", Description = "Найти далее (F3)", GeometryData = Geometry.Parse("M 2 10 L 18 10 M 14 6 L 18 10 L 14 14") },
        new ArrowItem { Name = "FindPrevious", Caption = "Найти предыдущее", Gesture = "Shift+F3", Description = "Найти предыдущее (Shift+F3)", GeometryData = Geometry.Parse("M 18 10 L 2 10 M 6 6 L 2 10 L 6 14") },
        new ArrowItem { Name = "FindAll", Caption = "Найти все", Description = "Найти все", GeometryData = Geometry.Parse("M 6 5 A 5 5 0 1 1 4.99 5.5 Z M 11 12 L 16 16.3") },
      };
    }

    /// <summary>
    /// Запускает выбранное действие левой кнопкой мыши при закрытом списке.
    /// </summary>
    /// <param name="sender">Значок выбранного действия.</param>
    /// <param name="e">Параметры нажатия кнопки мыши.</param>
    private void PART_ContentPresenter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || searchArrowsComboBox.IsDropDownOpen)
      {
        return;
      }

      if (searchArrowsComboBox.SelectedItem is ArrowItem action)
      {
        e.Handled = true;
        SearchEventAdapter.RaiseSearchButtonPressed(action.Name);
      }
    }
  }
}
