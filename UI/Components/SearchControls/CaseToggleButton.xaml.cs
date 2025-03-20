using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Кастомный элемент управления для переключения состояния (toggle button) кейса.
  /// </summary>
  public partial class CaseToggleButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CaseToggleButton"/>.
    /// </summary>
    public CaseToggleButton()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Получает или устанавливает состояние переключателя.
    /// Если значение <c>true</c>, текст окрашивается в красный, иначе — в черный.
    /// </summary>
    public bool IsChecked { get => GetChecked(); set => SetChecked(value); }

    /// <summary>
    /// Устанавливает состояние переключателя и изменяет цвет текста.
    /// Если <paramref name="value"/> равно <c>true</c>, цвет устанавливается в красный, иначе — в черный.
    /// </summary>
    /// <param name="value">Новое состояние переключателя.</param>
    private void SetChecked(bool value)
    {
      if (value)
      {
        ToggleButton.Foreground = Brushes.Red;
      }
      else
      {
        ToggleButton.Foreground = Brushes.Black;
      }
    }

    /// <summary>
    /// Возвращает текущее состояние переключателя.
    /// </summary>
    /// <returns><c>true</c>, если цвет текста равен красному; иначе <c>false</c>.</returns>
    private bool GetChecked()
    {
      if (ToggleButton.Foreground == Brushes.Red)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Обрабатывает событие предварительного нажатия кнопки мыши на переключателе.
    /// Переключает состояние элемента управления.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события мыши.</param>
    private void ToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      IsChecked = !IsChecked;
    }
  }
}
