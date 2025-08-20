using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
      private set
      {
        if (_currentDateTime != value)
        {
          _currentDateTime = value;
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentDateTime)));
        }
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EmptyWorkspaceView()
    {
      InitializeComponent();

      // стартовые значения
      CurrentDateTime = DateTime.Now;

      // тикаем каждую секунду
      _timer = new DispatcherTimer(DispatcherPriority.Background)
      {
        Interval = TimeSpan.FromSeconds(1)
      };
      _timer.Tick += (_, __) => CurrentDateTime = DateTime.Now;
      _timer.Start();

      // корректный стоп при выгрузке
      Unloaded += (_, __) => _timer.Stop();
    }
  }
}
