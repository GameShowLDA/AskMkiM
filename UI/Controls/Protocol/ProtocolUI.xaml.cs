using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static Utilities.LoggerUtility;

namespace UI.Controls.Protocol
{
  /// <summary>
  /// Логика взаимодействия для ProtocolUI.xaml
  /// </summary>
  public partial class ProtocolUI : UserControl
  {
    /// <summary>
    /// Устанавливает или возвращает текст заголовка.
    /// </summary>
    /// <param name="headerName">Текст заголовка.</param>
    public string Header
    {
      get
      {
        return this.header.Dispatcher.Invoke(() => this.header.Text);
      }
      set
      {
        if (this.header.Dispatcher.CheckAccess())
        {
          this.header.Text = value;
        }
        else
        {
          this.header.Dispatcher.BeginInvoke(new Action(() => this.header.Text = value));
        }
      }
    }

    public ObservableCollection<object> Items { get; }

    /// <summary>
    /// Добавляет элемент управления в динамическое содержимое.
    /// </summary>
    /// <param name="control">Элемент управления для добавления.</param>
    public void AddContent(UIElement control)
    {
      if (DynamicContentControl.Dispatcher.CheckAccess())
      {
        DynamicContentControl.Items.Add(control);
        if (DynamicContentControl.Items.Count > 0)
        {
          DynamicContentControl.Margin = new Thickness(20);
        }
      }
      else
      {
        DynamicContentControl.Dispatcher.Invoke(() =>
        {
          DynamicContentControl.Items.Add(control);
          if (DynamicContentControl.Items.Count > 0)
          {
            DynamicContentControl.Margin = new Thickness(20);
          }
        });
      }
    }

    /// <summary>
    /// Очищает динамическое содержимое.
    /// </summary>
    public async Task ClearContent()
    {
      LogInformation("Начало отчистки от содержимого");
      try
      {
        await DynamicContentControl.Dispatcher.InvokeAsync(() =>
        {
          DynamicContentControl.Items.Clear();
          if (DynamicContentControl.Items.Count == 0)
          {
            DynamicContentControl.Margin = new Thickness(20, 20, 20, 0);
          }
        });
        LogInformation("Отчистка от содержимого завершена");
      }
      catch (Exception ex)
      {
        LogError($"Ошибка отчистки содержимого: {ex}");
      }

    }

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolUI()
    {
      InitializeComponent();
      loopButton.Visibility = Visibility.Collapsed;
      returnButton.Visibility = Visibility.Collapsed;
      SetupButtons();
      ActionExecutor = Task.Run(() => ActionExecutor.CreateInstanceAsync(this)).Result;
      Items = new ObservableCollection<object>();
    }
  }
}
