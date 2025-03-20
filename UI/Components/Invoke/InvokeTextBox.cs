using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.Invoke
{
  /// <summary>
  /// Кастомный класс TextBox, обеспечивающий потокобезопасный доступ к его свойствам с использованием Dispatcher.
  /// Этот класс полезен для UI-компонентов, которые могут обновляться с не-UI потоков.
  /// </summary>
  public class InvokeTextBox : TextBox
  {
    /// <summary>
    /// Получает или устанавливает текстовое содержимое TextBox.
    /// Свойство потокобезопасно и может быть доступно или изменено с не-UI потоков.
    /// </summary>
    public new string Text
    {
      get
      {
        string text = string.Empty;
        Application.Current.Dispatcher.Invoke(() => text = base.Text);
        return text;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(() => base.Text = value);
      }
    }

    /// <summary>
    /// Получает или устанавливает цвет рамки TextBox.
    /// Свойство потокобезопасно и может быть доступно или изменено с не-UI потоков.
    /// </summary>
    public new Brush BorderBrush
    {
      get
      {
        Brush brush = null;
        Application.Current.Dispatcher.Invoke(() => brush = base.BorderBrush);
        return brush;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(() => base.BorderBrush = value);
      }
    }

    /// <summary>
    /// Получает или устанавливает состояние только для чтения для TextBox.
    /// Свойство потокобезопасно и может быть доступно или изменено с не-UI потоков.
    /// </summary>
    public new bool IsReadOnly
    {
      get
      {
        bool readOnly = false;
        Application.Current.Dispatcher.Invoke(() => readOnly = base.IsReadOnly);
        return readOnly;
      }

      set
      {
        Application.Current.Dispatcher.Invoke(() => base.IsReadOnly = value);
      }
    }
  }
}
