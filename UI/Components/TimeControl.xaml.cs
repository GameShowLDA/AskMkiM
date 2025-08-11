using System;
using System.Collections.Generic;
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

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для TimeControl.xaml
  /// </summary>
  public partial class TimeControl : UserControl
  {
    /// <summary>
    /// Свойство зависимости, определяющее, отображается ли поле времени.
    /// </summary>
    public static readonly DependencyProperty IsFullTimeProperty =
        DependencyProperty.Register(nameof(FullTime), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    public event Action ChangeDate;


    public bool FullTime { get; set; }
    public TimeControl()
    {
      InitializeComponent();
      Loaded += TimeControl_Loaded;

    }
    readonly DispatcherTimer DispatcherTimer = new DispatcherTimer();
    private void TimeControl_Loaded(object sender, RoutedEventArgs e)
    {
      Clock.Text = DateTime.Now.ToShortTimeString();
      DispatcherTimer.Interval = new TimeSpan(0, 0, 1);
      DispatcherTimer.Tick += DispatcherTimer_Tick;
      DispatcherTimer.Start();
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
      if (!FullTime)
      {
        Clock.Text = DateTime.Now.ToShortTimeString();
      }
      else
      {
        Clock.Text = DateTime.Now.ToLongTimeString();
      }

      if (Clock.Text == "00:00")
      {
        ChangeDate?.Invoke();
      }
    }
  }
}
