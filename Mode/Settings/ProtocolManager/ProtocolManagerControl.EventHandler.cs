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
