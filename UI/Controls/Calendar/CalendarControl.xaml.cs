using System.Windows.Controls;

namespace UI.Controls.Calendar
{
  /// <summary>
  /// Логика взаимодействия для CalendarControl.xaml
  /// </summary>
  public partial class CalendarControl : UserControl
  {
    private readonly CalendarViewModel _viewModel;

    public event EventHandler? SelectedDateChanged;

    public DateTime SelectedDate => _viewModel.SelectedDate;

    public void SetAvailabilityProvider(Func<DateTime, CalendarDayAvailability> provider)
    {
      _viewModel.AvailabilityProvider = provider;
      _viewModel.Refresh();
    }

    public CalendarControl()
    {
      InitializeComponent();
      _viewModel = new CalendarViewModel();
      _viewModel.SelectedDateChanged += (_, _) => SelectedDateChanged?.Invoke(this, EventArgs.Empty);
      DataContext = _viewModel;
    }
  }
}
