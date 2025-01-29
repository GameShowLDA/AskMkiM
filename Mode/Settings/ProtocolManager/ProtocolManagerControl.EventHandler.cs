using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mode.Settings.ProtocolManager
{
  public partial class ProtocolManagerControl
  {
    private void Switch_Checked(object sender, RoutedEventArgs e)
    {
      NewDataSaveAsync();
    }
  }
}
