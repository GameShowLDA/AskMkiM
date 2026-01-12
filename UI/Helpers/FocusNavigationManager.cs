using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Helpers
{
  /// <summary>
  /// Менеджер управления клавиатурной навигацией по элементам управления.
  /// Позволяет переключаться между элементами с помощью клавиш Up/Down и активировать кнопки клавишей Enter.
  /// </summary>
  public class FocusNavigationManager
  {
    /// <summary>
    /// Коллекция элементов управления, участвующих в навигации.
    /// </summary>
    private readonly List<Control> _focusElements = new();

    /// <summary>
    /// Корневой элемент, на котором обрабатываются события клавиш.
    /// </summary>
    private readonly UIElement _root;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FocusNavigationManager"/>.
    /// </summary>
    /// <param name="root">Корневой элемент, на котором обрабатываются события клавиатуры.</param>
    public FocusNavigationManager(UIElement root)
    {
      _root = root;
      if (_root is FrameworkElement fe)
        fe.PreviewKeyDown += OnKeyDown;
    }

    /// <summary>
    /// Добавляет элемент управления в список навигации.
    /// </summary>
    /// <param name="control">Элемент управления для добавления.</param>
    public void Add(Control control)
    {
      if (!_focusElements.Contains(control))
        _focusElements.Add(control);
    }

    /// <summary>
    /// Добавляет несколько элементов управления в список навигации.
    /// </summary>
    /// <param name="controls">Коллекция элементов управления для добавления.</param>
    public void AddRange(IEnumerable<Control> controls)
    {
      foreach (var control in controls)
        Add(control);
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш и выполняет переключение фокуса или активацию кнопок.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиш.</param>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      var focused = Keyboard.FocusedElement as Control;
      if (focused == null) return;

      int index = _focusElements.IndexOf(focused);
      if (index == -1) return;

      if (e.Key == Key.Down)
      {
        int next = (index + 1) % _focusElements.Count;
        _focusElements[next]?.Focus();
        e.Handled = true;
      }
      else if (e.Key == Key.Up)
      {
        int prev = (index - 1 + _focusElements.Count) % _focusElements.Count;
        _focusElements[prev]?.Focus();
        e.Handled = true;
      }
      else if (e.Key == Key.Enter && focused is Button button)
      {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        e.Handled = true;
      }
    }

    /// <summary>
    /// Отключает обработку событий клавиатуры и очищает список элементов навигации.
    /// </summary>
    public void Detach()
    {
      if (_root is FrameworkElement fe)
        fe.KeyDown -= OnKeyDown;

      _focusElements.Clear();
    }
  }
}
