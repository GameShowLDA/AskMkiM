using Ask.Engine.Tests.RelaySwitchingModule;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.TestsControls
{
  /// <summary>
  /// Логика взаимодействия для RkommConnectionControl.xaml
  /// </summary>
  public partial class RkommConnectionControl : UserControl
  {

    private RkommConnectionTests relayContactResistConnection = new();

    public RkommConnectionControl()
    {
      InitializeComponent();

      _ = relayContactResistConnection.InitializeSettingsAsync(ProtocolSelfCheckControl, ProtocolSelfCheckControl, ProtocolSelfCheckControl);
    }
  }
}
