using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.TextEditor;
using static UI.Controls.StatusBarControl;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для нижней панели редактора.
  /// </summary>
  public class TextEditorStatusViewModel : INotifyPropertyChanged, ICommandProvider
  {
    private int _line;
    private int _column;
    private int _lineCount;
    private string _encodingName = "UTF-8";

    public int Line
    {
      get => _line;
      set { _line = value; OnPropertyChanged(nameof(Line)); }
    }

    public int Column
    {
      get => _column;
      set { _column = value; OnPropertyChanged(nameof(Column)); }
    }

    public int LineCount
    {
      get => _lineCount;
      set { _lineCount = value; OnPropertyChanged(nameof(LineCount)); }
    }

    public string EncodingName
    {
      get => _encodingName;
      set { _encodingName = value; OnPropertyChanged(nameof(EncodingName)); }
    }

    public Func<TextEditorUI?>? GetActiveEditor { get; set; }

    public Encoding CurrentEncoding { get; private set; } = Encoding.UTF8;

    public ICommand ChangeEncodingCommand { get; }
    public ICommand ToggleEncodingCommand { get; }

    public TextEditorStatusViewModel()
    {
      ChangeEncodingCommand = new ChangeEncodingMouseCommand(this);
      ToggleEncodingCommand = new ToggleEncodingCommandImpl(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Команда смены кодировки через контекстное меню по ПКМ.
    /// </summary>
    private class ChangeEncodingMouseCommand : ICommand
    {
      private readonly TextEditorStatusViewModel _viewModel;

      public ChangeEncodingMouseCommand(TextEditorStatusViewModel viewModel)
      {
        _viewModel = viewModel;
      }

      public event EventHandler? CanExecuteChanged;
      public bool CanExecute(object? parameter) => true;

      public void Execute(object? parameter)
      {
        var menu = new ContextMenu();
        var encodings = new[]
        {
        Encoding.UTF8,
        Encoding.Unicode,               // UTF-16 LE
        Encoding.BigEndianUnicode,     // UTF-16 BE
        Encoding.ASCII,
        Encoding.UTF32,
        Encoding.GetEncoding("windows-1251"),   // Кириллица (ANSI)
        Encoding.GetEncoding("windows-1252"),   // Западноевропейская (ANSI)
        Encoding.GetEncoding("iso-8859-1"),     // Latin1
        Encoding.GetEncoding("iso-8859-5"),     // Кириллица (ISO)
        Encoding.GetEncoding("koi8-r"),
        Encoding.GetEncoding("koi8-u"),        // Украинская (КОИ8-U)
        Encoding.GetEncoding("macintosh"),     // Mac Roman
        Encoding.GetEncoding("IBM855"),        // OEM 855
        Encoding.GetEncoding("IBM866"),        // OEM 866
        Encoding.GetEncoding("windows-1251")
        };

        foreach (var enc in encodings)
        {
          var item = new MenuItem { Header = enc.EncodingName, Tag = enc };
          item.Click += (_, _) =>
          {
            var selectedEncoding = (Encoding)item.Tag;

            // Обновим в ViewModel
            _viewModel.CurrentEncoding = selectedEncoding;
            _viewModel.EncodingName = selectedEncoding.WebName.ToUpperInvariant();

            // Получаем активный редактор
            var editor = _viewModel.GetActiveEditor?.Invoke();
            if (editor == null)
            {
              MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return;
            }

            var path = editor.TextEditorModel.FilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
              MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return;
            }

            try
            {
              string content = File.ReadAllText(path, selectedEncoding);
              editor.TextEditorModel.Encoding = selectedEncoding;
              editor.TextEditor.Text = content;
            }
            catch (Exception ex)
            {
              MessageBox.Show("Ошибка при чтении файла в новой кодировке:\n" + ex.Message,
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
          };
          menu.Items.Add(item);
        }

        menu.IsOpen = true;
      }

    }

    private class ToggleEncodingCommandImpl : ICommand
    {
      private readonly TextEditorStatusViewModel _viewModel;
      private static readonly Encoding Utf8 = Encoding.UTF8;
      private static readonly Encoding Dos866 = Encoding.GetEncoding("IBM866");

      public ToggleEncodingCommandImpl(TextEditorStatusViewModel viewModel)
      {
        _viewModel = viewModel;
      }

      public event EventHandler? CanExecuteChanged;
      public bool CanExecute(object? parameter) => true;

      public void Execute(object? parameter)
      {
        var editor = _viewModel.GetActiveEditor?.Invoke();
        if (editor == null)
        {
          MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        var current = _viewModel.CurrentEncoding.WebName.ToUpperInvariant();
        var utfEncoding = Utf8.WebName.ToUpperInvariant();
        var newEncoding = current == utfEncoding ? Dos866 : Utf8;

        var path = editor.TextEditorModel.FilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
          MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        try
        {
          string content = File.ReadAllText(path, newEncoding);
          editor.TextEditorModel.Encoding = newEncoding;
          editor.TextEditor.Text = content;

          _viewModel.CurrentEncoding = newEncoding;
          _viewModel.EncodingName = newEncoding.WebName.ToUpperInvariant();
        }
        catch (Exception ex)
        {
          MessageBox.Show("Ошибка при чтении файла в новой кодировке:\n" + ex.Message,
                          "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }
  }
}
