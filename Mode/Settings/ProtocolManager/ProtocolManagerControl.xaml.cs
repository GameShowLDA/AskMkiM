using System.Windows.Controls;

namespace Mode.Settings.ProtocolManager
{
  /// <summary>
  /// Логика взаимодействия для ProtocolManagerControl.xaml
  /// </summary>
  public partial class ProtocolManagerControl : UserControl
  {
    public ProtocolManagerControl()
    {
      InitializeComponent();
      SetConfiguration();
      start = true;
    }
  }
}
