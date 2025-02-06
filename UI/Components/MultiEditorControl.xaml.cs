using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.Invoke;
using UI.Controls.TextEditor;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiEditorControl.xaml
  /// </summary>
  public partial class MultiEditorControl : UserControl
  {
    List<OpenFileButton> openPages = new List<OpenFileButton>();
    List<UserControl> userControls = new List<UserControl>();

    private int _clickCount = 0;
    private DispatcherTimer _clickTimer;

    public MultiEditorControl()
    {
      InitializeComponent();
      _clickTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(300)
      };

      _clickTimer.Tick += (s, e) =>
      {
        _clickCount = 0;
        _clickTimer.Stop();
      };

      this.KeyDown += MultiWindowControl_KeyDown;
    }

    private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _clickCount++;

      if (_clickCount == 1)
      {
        _clickTimer.Start();
      }
      else if (_clickCount == 2)
      {
        _clickTimer.Stop();
        _clickCount = 0;
        CreateNewFile();
      }
    }

    /// <summary>
    /// Добавляет элемент управления и кнопку в соответствующие панели.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    public void AddControl(string header, UserControl control, string description = null)
    {
      OpenFileButton tabButton = new OpenFileButton();
      tabButton.Header.Text = header;
      if (description != null)
      {
        tabButton.Description = description;

        foreach (OpenFileButton page in openPages)
        {
          if (page.Description == description)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);
            return;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in openPages)
        {
          if (page.Header.Text == header)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);
            return;
          }
        }
      }



      tabButton.PreviewMouseDown += (s, e) => ShowControl(control, tabButton);
      tabButton.GetCloseButton().PreviewMouseDown += (s, e) => RemoveControl(tabButton, control);
      tabButton.MouseDown += (s, e) =>
      {
        if (e.ChangedButton == MouseButton.Middle)
        {
          RemoveControl(tabButton, control);
        }
      };

      openPages.Add(tabButton);
      userControls.Add(control);

      try
      {
        ContentPanel.Children.Add(control);
        TopPanel.Children.Add(tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
    }

    public void OpenFile(string path)
    {
      var nameFile = GetNameFile(path);
      if (string.IsNullOrEmpty(nameFile))
      {
        MessageBox.Show("Ошибка", "Ошибка при открытии файла");
        return;
      }

      try
      {
        string fileContent = System.IO.File.ReadAllText(path);

        var textEditor = new TextEditorUI();
        textEditor.Text = fileContent;

        AddControl(nameFile, textEditor);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
      }
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      AddControl("Новый", new TextEditorUI() /*{ Text  = "Новый файл"}*/);
    }

    /// <summary>
    /// Получает имя файла по пути к файлу.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetNameFile(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return string.Empty;
      }
      try
      {
        return System.IO.Path.GetFileName(path).ToString();
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ActivePage(OpenFileButton control)
    {
      foreach (OpenFileButton child in TopPanel.Children)
      {
        if (control == child)
        {
          child.Background = (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
        }
        else
        {
          child.Background = (Brush)Application.Current.Resources["SecondarySolidColorBrush"];
        }
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ShowControl(UserControl control, OpenFileButton openPage)
    {
      foreach (UIElement child in ContentPanel.Children)
      {
        child.Visibility = child == control ? Visibility.Visible : Visibility.Collapsed;
      }

      ActivePage(openPage);
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      if (openPages.Contains(tabButton) && userControls.Contains(control))
      {
        int index = ContentPanel.Children.IndexOf(control);
        if (index > 0)
        {
          index--;
        }

        openPages.Remove(tabButton);
        userControls.Remove(control);

        TopPanel.Children.Remove(tabButton);
        ContentPanel.Children.Remove(control);

        if (ContentPanel.Children.Count > 0)
        {
          ShowControl(userControls[index], openPages[index]);
        }
      }
    }

    private void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      Console.WriteLine($"e.Key = {e.Key}; e.SystemKey = {e.SystemKey}; Keyboard.Modifiers = {Keyboard.Modifiers}");

      if (e.Key == Key.System && e.SystemKey == Key.X && Keyboard.Modifiers == ModifierKeys.Alt)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int index = openPages.IndexOf(activeTab);
          RemoveControl(activeTab, userControls[index]);
        }
      }
    }
  }
}
