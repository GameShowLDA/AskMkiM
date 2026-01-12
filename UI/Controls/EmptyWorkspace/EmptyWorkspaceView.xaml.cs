using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI.Controls.EmptyWorkspace
{
  /// <summary>
  /// Логика взаимодействия для EmptyWorkspaceView.xaml
  /// </summary>
  public partial class EmptyWorkspaceView : UserControl, INotifyPropertyChanged
  {
    private readonly DispatcherTimer _timer;
    private DateTime _currentDateTime;

    public DateTime CurrentDateTime
    {
      get => _currentDateTime;
      set
      {
        if (_currentDateTime != value)
        {
          _currentDateTime = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentDateTime)));
        }
      }
    }

    private string _buildDate;
    public string BuildDate
    {
      get => _buildDate;
      set
      {
        if (_buildDate != value)
        {
          _buildDate = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BuildDate)));
        }
      }
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    public EmptyWorkspaceView()
    {
      InitializeComponent();

      CurrentDateTime = DateTime.Now;
      BuildDate = GetBuildDate();

      // тикаем каждую секунду
      _timer = new DispatcherTimer(DispatcherPriority.Background)
      {
        Interval = TimeSpan.FromMilliseconds(100)
      };
      _timer.Tick += (_, __) => CurrentDateTime = DateTime.Now;
      _timer.Start();

      // корректный стоп при выгрузке
      Unloaded += (_, __) => _timer.Stop();
    }
    private string GetBuildDate()
    {
      try
      {
        var asm = System.Reflection.Assembly.GetEntryAssembly();

        var attr = asm.GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
                      .Cast<AssemblyMetadataAttribute>()
                      .FirstOrDefault(a => a.Key == "BuildDate");

        return attr?.Value ?? "Неизвестно";
      }
      catch
      {
        return "Ошибка";
      }
    }
  }
}
