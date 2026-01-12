using Ask.Engine.Tests.SelfControl;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.SelfControl
{
  /// <summary>
  /// Логика взаимодействия для SystemSelfControl.xaml
  /// </summary>
  public partial class SystemSelfControl : UserControl
  {

    public SystemSelfControl()
    {
      InitializeComponent();
      new SystemSelfExecutor().InitializeSettings(ProtocolUI);
    }
  }
}
