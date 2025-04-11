using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.ProtocolController.Execution;
using UI.Controls.ProtocolController.Export;
using UI.Controls.ProtocolController.Message;
using static Utilities.DelegateManager;
using Utilities;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Класс управления пользовательским интерфейсом протокола выполнения.
  /// Обеспечивает взаимодействие с пользователем, управление процессами и обработку сообщений.
  /// </summary>
  public partial class ProtocolController : UserControl
  {
    /// <summary>
    /// Свойство зависимости для заголовка.
    /// Позволяет изменять заголовок через XAML или код.
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(ProtocolController),
            new PropertyMetadata(string.Empty, OnHeaderChanged));

    /// <summary>
    /// Получает или задает текст заголовка.
    /// </summary>
    public string Header
    {
      get => (string)GetValue(HeaderProperty);
      set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Обработчик изменения заголовка, чтобы обновлять TextBlock.
    /// </summary>
    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ProtocolController control)
      {
        control.header.Text = e.NewValue as string;
      }
    }

    /// <summary>
    /// Свойство зависимости для установки динамического контента.
    /// Позволяет добавить стандартный элемент (Button, TextBlock и т. д.) или UserControl.
    /// </summary>
    public static readonly DependencyProperty ContentViewProperty =
        DependencyProperty.Register(
            nameof(ContentView),
            typeof(FrameworkElement),
            typeof(ProtocolController),
            new PropertyMetadata(null));

    /// <summary>
    /// Получает или задает контент (стандартный элемент или UserControl).
    /// </summary>
    public FrameworkElement ContentView
    {
      get => (FrameworkElement)GetValue(ContentViewProperty);
      set => SetValue(ContentViewProperty, value);
    }

    /// <summary>
    /// Команда для установки динамического контента из XAML.
    /// </summary>
    public ICommand SetContentCommand { get; }

    /// <summary>
    /// Коллекция элементов, используемых в протоколе.
    /// </summary>
    public ObservableCollection<object> Items { get; }

    public MessageManager MessageManager { get; private set; }

    public ProtocolExportService ExportService { get; private set; }

    public PauseManager PauseManager { get; private set; }

    public DelegateRegistry DelegateRegistry { get; private set; }

    /// <summary>
    /// Конструктор по умолчанию для элемента ProtocolSelfCheck.
    /// Инициализирует компоненты и устанавливает обработчики событий PreviewMouseDown для кнопок.
    /// </summary>
    public ProtocolController()
    {
      InitializeComponent();
      this.DataContext = this;

      loopButton.Visibility = Visibility.Collapsed;
      returnButton.Visibility = Visibility.Collapsed;
      Items = new ObservableCollection<object>();

      AppConfiguration.Services.UserMessageServiceProvider.Instance = this;

      SetupButtons();
      ActionExecutor = Task.Run(() => ActionExecutor.CreateInstanceAsync(this)).Result;

      MessageManager = new MessageManager(protocolTextBox, ActionExecutor, this);
      ExportService = new ProtocolExportService(protocolTextBox, Header);
      PauseManager = new PauseManager(MessageManager, this);
      DelegateRegistry = new DelegateRegistry();
    }

    /// <summary>
    /// Устанавливает основные настройки выполнения действий.
    /// </summary>
    /// <param name="MainWindow">Главное окно приложения.</param>
    /// <param name="StartDelegate">Делегат запуска.</param>
    /// <param name="isRepeatEnabled">Флаг разрешения повторного выполнения.</param>
    /// <param name="StopDelegate">Делегат остановки (необязательно).</param>
    /// <param name="ReturnDelegate">Делегат возврата к предыдущему состоянию (необязательно).</param>
    /// <param name="preActionDelegate">Делегат предварительных действий перед запуском (необязательно).</param>
    public void SetSettings(UIElement MainWindow, StartDelegate StartDelegate, bool isRepeatEnabled, StopDelegate StopDelegate = null, ReturnDelegate ReturnDelegate = null, PreActionDelegate preActionDelegate = null)
    {
      try
      {
        _mainWindow = MainWindow;
        DelegateRegistry.StopDelegate = StopDelegate;
        DelegateRegistry.StartDelegate = StartDelegate;
        DelegateRegistry.ReturnDelegate = ReturnDelegate;
        DelegateRegistry.PreActionDelegate = preActionDelegate;

        if (ReturnDelegate != null)
        {
          DelegateRegistry.IsRepeatEnabled = true;
        }
      }
      catch (Exception ex)
      {
        LoggerUtility.LogException("Ошибка загрузки элемента", ex);
        throw;
      }
    }
  }
}
