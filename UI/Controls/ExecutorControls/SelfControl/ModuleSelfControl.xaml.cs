using Ask.Engine.Tests.SelfControl;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.SelfControl
{
  /// <summary>
  /// Логика взаимодействия для ModuleSelfControl.xaml
  /// </summary>
  public partial class ModuleSelfControl : UserControl
  {
    public ModuleSelfControl()
    {
      InitializeComponent();
      new ModuleSelfExecutor(ProtocolUI).InitializeSettings(ProtocolUI);
    }
  }
}
