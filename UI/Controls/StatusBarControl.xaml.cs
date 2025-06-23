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
    }

    //private void EncodingTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    //{
    //  if (DataContext is ICommandProvider provider && provider.ChangeEncodingCommand.CanExecute(e))
    //  {
    //    provider.ChangeEncodingCommand.Execute(e);
    //  }
    //}
    private void EncodingTextBlock_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (DataContext == null)
      {
        MessageBox.Show("DataContext is null");
        return;
      }

      if (DataContext is not ICommandProvider provider)
      {
        MessageBox.Show("DataContext is not ICommandProvider");
        return;
      }

      if (provider.ChangeEncodingCommand == null)
      {
        MessageBox.Show("ChangeEncodingCommand is null");
        return;
      }

      if (!provider.ChangeEncodingCommand.CanExecute(e))
      {
        MessageBox.Show("Command can't execute");
        return;
      }

      provider.ChangeEncodingCommand.Execute(e);
    }

  }
}
