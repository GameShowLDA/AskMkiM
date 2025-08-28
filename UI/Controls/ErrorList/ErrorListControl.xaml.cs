using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Utilities.Errors;
using Utilities.Models;

namespace UI.Controls.ErrorList
{
  /// <summary>
  /// Логика взаимодействия для ErrorListControl.xaml
  /// </summary>
  public partial class ErrorListControl : UserControl
  {
    public ObservableCollection<ErrorItem> Errors { get; } = new();
    public ErrorListControl()
    {
      InitializeComponent();
      DataContext = this;
    }

    public Visibility StringsNumberVisible 
    {
      get
      {
        return StringsNumber.Visibility;
      }
      set
      { 
        StringsNumber.Visibility = value;
      }
    }

    public Visibility MeasureResultVisible
    {
      get
      {
        return MeasureResult.Visibility;
      }
      set
      {
        MeasureResult.Visibility = value;
      }
    }

    /// <summary>
    /// Событие вызывается при двойном клике по строке с ошибкой.
    /// </summary>
    public event Action<ErrorItem>? ErrorItemDoubleClicked;

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGrid grid && grid.SelectedItem is ErrorItem selectedError)
      {
        ErrorItemDoubleClicked?.Invoke(selectedError);
      }
    }
  }
}
