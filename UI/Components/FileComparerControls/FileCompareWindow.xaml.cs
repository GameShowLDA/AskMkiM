using AppConfiguration.Base;
using System.Windows;
using System.Windows.Input;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareWindow.xaml
  /// </summary>
  public partial class FileCompareWindow : Window
  {
    /// <summary>
    /// Событие, возникающее при закрытии диалога.
    /// </summary>
    public event EventHandler DialogClosed;


    /// <summary>
    /// Определяет, разрешено ли закрытие окна или диалога.
    /// </summary>
    private bool _allowClose;

    public FileCompareWindow()
    {
      InitializeComponent();
      Owner = Application.Current.MainWindow;

      ShowInTaskbar = false;
      WindowStyle = WindowStyle.None;
      ResizeMode = ResizeMode.NoResize;

      this.Closed += (s, e) =>
      {
        if (Owner != null)
        {
          Owner.IsEnabled = true;
          Owner.Focus();
          DialogClosed?.Invoke(this, EventArgs.Empty);
        }
      };

      this.Closing += (s, e) =>
      {
        if (!_allowClose)
        {
          e.Cancel = true;
        }
      };

      this.Deactivated += (s, e) =>
      {
        this.Activate();
        this.Focus();
      };
    }

    public new bool? ShowDialog()
    {
      this.Activate();
      this.Focus();

      return base.ShowDialog();
    }

    private void CompareButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {

    }
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        this.DragMove();
      }
    }

    private void CloseButton_Click(object sender, MouseButtonEventArgs e)
    {
      CloseDialog();
    }

    public void CloseDialog()
    {
      _allowClose = true;
      this.Close();
    }
  }
}
