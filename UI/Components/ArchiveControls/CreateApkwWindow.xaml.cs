using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using UI.Components.ArchiveManager;
using AppConfiguration.Base;

namespace UI.Components.ArchiveControls
{
  /// <summary>
  /// Логика взаимодействия для CreateApkwWindow.xaml
  /// </summary>
  public partial class CreateApkwWindow : Window
  {
    public static bool IsMainActive
    {
      get;
      set;
    }
    private bool _allowClose;
    public event EventHandler DialogClosed;

    private void NewArchiveName_GotFocus(object sender, RoutedEventArgs e)
    {
      if (newArchiveName.Text == (string)newArchiveName.Tag)
      {
        newArchiveName.Text = string.Empty;
        newArchiveName.Foreground = Brushes.Black;
      }
    }

    private void NewArchiveName_LostFocus(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(newArchiveName.Text))
      {
        newArchiveName.Text = (string)newArchiveName.Tag;
        newArchiveName.Foreground = Brushes.Gray;
      }
    }
    private void NewArchiveDescription_GotFocus(object sender, RoutedEventArgs e)
    {
      string richtextValue = GetRichTexBoxValue(newArchiveDescription);

      if (richtextValue == (string)newArchiveDescription.Tag)
      {
        newArchiveDescription.Document.Blocks.Clear();
        newArchiveDescription.Document.Blocks.Add(new Paragraph(new Run(string.Empty)));
        newArchiveDescription.Foreground = Brushes.Black;
      }
    }

    private void NewArchiveDescription_LostFocus(object sender, RoutedEventArgs e)
    {
      string richtextValue = GetRichTexBoxValue(newArchiveDescription);

      if (string.IsNullOrWhiteSpace(richtextValue))
      {
        newArchiveDescription.Document.Blocks.Clear();
        newArchiveDescription.Document.Blocks.Add(new Paragraph(new Run((string)newArchiveDescription.Tag)));
        newArchiveDescription.Foreground = Brushes.Gray;
      }
    }

    public CreateApkwWindow()
    {
      InitializeComponent();
      IsMainActive = false;
      DefaultGotAndLostEvent(newArchiveName, newArchiveName.Tag.ToString());
      DefaultGotAndLostEvent(newArchiveDescription, newArchiveDescription.Tag.ToString());
      Owner = Application.Current.MainWindow;
      EventAggregator.AdminRightsChanged += ApplicationDataHandler_AdminRightsChanged;

      ShowInTaskbar = false;
      WindowStyle = WindowStyle.None;
      ResizeMode = ResizeMode.NoResize;

      if (EventAggregator.GetAdminRights())
      {
        choseArchiveType.Visibility = Visibility.Visible;
      }

      if (Owner != null)
      {
        Owner.IsEnabled = false;
      }

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

    public void CloseDialog()
    {
      _allowClose = true;
      this.Close();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
        this.DragMove();
    }

    private async void CreateApkButton_Click(object sender, MouseButtonEventArgs e)
    {
      string archiveName;
      string archiveDescription;
      if (newArchiveName is TextBox nameTextBox)
      {
        archiveName = nameTextBox.Text.Trim();
      }
      else
      {
        return;
      }

      if (newArchiveDescription is RichTextBox descriptionTextBox)
      {
        var description = GetRichTexBoxValue(descriptionTextBox);
        if (description == descriptionTextBox.Tag.ToString())
        {
          archiveDescription = string.Empty;
        }
        else
        {
          archiveDescription = description;
        }
      }
      else
      {
        return;
      }

      if (ValidateName() == false)
      {
        return;
      }

      try
      {
        var archiveEditor = new ArchiveEditor();
        var mainChecked = main.IsChecked;
        if (mainChecked == null)
        {
          mainChecked = false;
        }
        var result = await archiveEditor.CreateArchive(ArchiveSettings.ArchivePath, archiveName, archiveDescription, (bool)mainChecked);
        if (result)
        {
          CloseDialog();
        }
        else
        {
          MessageBox.Show("Возникла ошибка при создании архива. Повторите попытку позднее.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при создании архива: {ex.Message}");
      }
    }

    private bool ValidateName()
    {
      if (string.IsNullOrWhiteSpace(newArchiveName.Text) || newArchiveName.Text.ToString() == newArchiveName.Tag.ToString())
      {
        MessageBox.Show("Пожалуйста, введите название архива");
        newArchiveName.Focus();
        return false;
      }

      if (newArchiveName.Text.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
      {
        MessageBox.Show("Название содержит недопустимые символы");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Настраивает события GotFocus и LostFocus для TextBox.
    /// </summary>
    /// <param name="textBox">TextBox для настройки.</param>
    /// <param name="defaultText">Текст по умолчанию для TextBox.</param>
    static public void DefaultGotAndLostEvent(TextBox textBox, string defaultText)
    {
      textBox.GotFocus += (sender, e) =>
      {
        if (textBox.Text == defaultText)
        {
          textBox.Text = string.Empty;
        }

      };

      textBox.LostFocus += (sender, e) =>
      {
        if (string.IsNullOrEmpty(textBox.Text))
        {
          textBox.Text = defaultText;
        }
      };
    }

    /// <summary>
    /// Настраивает события GotFocus и LostFocus для RichTextBox.
    /// </summary>
    /// <param name="textBox">TextBox для настройки.</param>
    /// <param name="defaultText">Текст по умолчанию для TextBox.</param>
    static public void DefaultGotAndLostEvent(RichTextBox textBox, string defaultText)
    {

      textBox.GotFocus += (sender, e) =>
      {
        string richtextValue = GetRichTexBoxValue(textBox);
        if (richtextValue == defaultText)
        {
          textBox.Document.Blocks.Clear();
          textBox.Document.Blocks.Add(new Paragraph(new Run(string.Empty)));
        }

      };

      textBox.LostFocus += (sender, e) =>
      {
        string richtextValue = GetRichTexBoxValue(textBox);
        if (string.IsNullOrEmpty(richtextValue))
        {
          textBox.Document.Blocks.Clear();
          textBox.Document.Blocks.Add(new Paragraph(new Run(defaultText)));
        }
      };
    }

    private static string GetRichTexBoxValue(RichTextBox textBox)
    {
      TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
      string richtextValue = textRange.Text;
      return richtextValue.Replace("\r\n", string.Empty);
    }

    private void CloseButton_Click(object sender, MouseButtonEventArgs e)
    {
      CloseDialog();
    }

    private void Switch_Checked(object sender, RoutedEventArgs e)
    {
      if (sender == main && additional.IsChecked == true)
      {
        additional.IsChecked = false;
      }
      else if (sender == additional && main.IsChecked == true)
      {
        main.IsChecked = false;
        IsMainActive = false;
      }
    }

    private void Switch_Unchecked(object sender, RoutedEventArgs e)
    {
      if (sender == main && additional.IsChecked == false)
      {
        additional.IsChecked = true;
      }
      else if (sender == additional && main.IsChecked == false)
      {
        main.IsChecked = true;
        IsMainActive = true;
      }
    }

    private void ApplicationDataHandler_AdminRightsChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue)
        {
          this.choseArchiveType.Visibility = Visibility.Visible;
        }
        else
        {
          this.choseArchiveType.Visibility = Visibility.Collapsed;
        }
      });
    }
  }
}
