using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Controls.Search;

public partial class SearchWindow
{
  /// <summary>Очищает поле поиска или замены целиком, сохраняя фокус ввода.</summary>
  public static readonly RoutedUICommand ClearFieldCommand = new("Очистить поле", nameof(ClearFieldCommand), typeof(SearchWindow));

  /// <summary>Подключает команды полей нового оформления к существующему окну.</summary>
  private void InitializeDesignCommands()
  {
    CommandBindings.Add(new CommandBinding(ClearFieldCommand, (_, args) =>
    {
      if (args.Parameter is TextBox textBox)
      {
        textBox.Clear();
        textBox.Focus();
      }
      args.Handled = true;
    }, (_, args) =>
    {
      args.CanExecute = args.Parameter is TextBox { IsReadOnly: false, Text.Length: > 0 };
      args.Handled = true;
    }));
  }

}
