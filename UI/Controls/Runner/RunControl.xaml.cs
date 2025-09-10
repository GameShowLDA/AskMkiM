using AppConfiguration.Base;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using Message;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Components;
using UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl
  {
    List<BaseCommandModel> ControlProgram = null;
    public int ErrorCount { get; private set; } = 0;
    private ProtocolUI ProtocolUI { get; set; }
    public string FileName { get; set; }
    public string HeaderFile
    {
      get
      {
        return headerFile.Text;
      }
      set
      {
        headerFile.Text = value;
      }
    }

    bool task = false;
    public RunControl()
    {
      InitializeComponent();
      ProtocolUI = new ProtocolUI(true);
      ProtocolUI.ErrorListBoxVerticalVisibility = Visibility.Collapsed;
      MainContent.Content = ProtocolUI;

      Loaded += RunControl_Loaded;
      LeftBox.AddHandler(UIElement.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(LeftBox_PreviewGotKeyboardFocus), true);
    }

    private void RunControl_Loaded(object sender, RoutedEventArgs e)
    {
      FocusMainContent();
    }
    private void LeftBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      // Отменяем фокусировку и возвращаем в MainContent
      e.Handled = true;
      FocusMainContent();
    }
    private void FocusMainContent()
    {
      if (MainContent.Content is IInputElement focusable && focusable.Focusable)
      {
        Keyboard.Focus(focusable);
      }
      else if (MainContent.Content is FrameworkElement fe)
      {
        fe.Loaded += (_, _) =>
        {
          fe.Focus();
          Keyboard.Focus(fe);
        };
      }
    }


    public void SetLeftEditor(TextEditorUI textEditorUI)
    {
      LogInformation("SetLeftEditor вызван: " + this.GetHashCode());

      if (textEditorUI == null)
        return;

      if (textEditorUI.Parent is Panel oldParent)
      {
        oldParent.Children.Remove(textEditorUI);
      }
      else if (textEditorUI.Parent is ContentControl oldContent)
      {
        oldContent.Content = null;
      }
      else if (textEditorUI.Parent is Decorator decorator)
      {
        decorator.Child = null;
      }

      LeftBox.Children.Clear();
      LeftBox.Children.Add(textEditorUI);
    }

    public void Start(List<BaseCommandModel> models)
    {
      ProtocolUI.MenuButtonVisibility(false);
      ControlProgram = models;

      var ok = models[0];
      if (ok.Mnemonic != "ОК")
      {
        return;
      }

      ProtocolUI.Header = (ok as OkCommandModel).ObjectCode;
      ProtocolUI.SetSettings(this, StartDelegate: StartTest, false);
      this.FileName = ProtocolUI.Header;
    }

    private async Task StartTest(CancellationToken cancellationToken)
    {
      TextEditorUI? editor = null;

      Application.Current.Dispatcher.Invoke(() =>
      {
        editor = LeftBox.Children[0] as TextEditorUI;
      });

      var manager = new CommandExecutionManager(ProtocolUI, editor, ControlProgram);
      manager.ClearError += ErrorClear;
      manager.AddError += AddError;

      await manager.ExecuteAllAsync();
    }
    private void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.Errors.Add(errorItem);
        ErrorCount++;

        if (ErrorCount > 0)
        {
          AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Общее кол-во ошибок: {ErrorCount}");
        }
      });
    }

    private void ErrorClear()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.Errors.Clear();
        ErrorCount = 0;
      });
    }

    private void PreviousArrow_ClickMouse(MouseButtonEventArgs obj)
    {
      //нужно получить активный текстовый редактор и открыть файл который расположен по пути в texteditormodel
      var test = this.LeftBox.Children[0];
      if (test != null && test is TextEditorUI textEditor)
      {
        if (textEditor.TextEditorModel != null 
          && !string.IsNullOrEmpty(textEditor.TextEditorModel.FilePath) 
          && File.Exists(textEditor.TextEditorModel.FilePath))
        {
          EventAggregator.RaiseOpenFileInEditorAgain(textEditor.TextEditorModel.FilePath);
          EventAggregator.RaiseCloseRunItem(this);
        }
      }
      else
      {
        MessageBoxCustom.Show("Ошибка обнаружения исходного файла", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }
  }
}
