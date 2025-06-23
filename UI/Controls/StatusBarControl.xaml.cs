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

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для StatusBarControl.xaml
  /// </summary>
  public partial class StatusBarControl : UserControl
  {
    public StatusBarControl()
    {
      InitializeComponent();
    }

    public interface ICommandProvider
    {
      ICommand ChangeEncodingCommand { get; }
      ICommand ToggleEncodingCommand { get; }
    }

    private void EncodingTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ICommandProvider provider && provider.ChangeEncodingCommand.CanExecute(e))
      {
        provider.ChangeEncodingCommand.Execute(e);
      }
    }

    private void EncodingTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (DataContext is ICommandProvider provider && provider.ChangeEncodingCommand.CanExecute(e))
      {
        provider.ToggleEncodingCommand.Execute(e);
      }
    }

  }
}
