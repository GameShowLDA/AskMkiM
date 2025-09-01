using System.Windows.Controls;
using Utilities.Help;

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

      // Регистрируем обработчик движения мыши
      MouseMove += (s, e) =>
      {
        // Обновляем последний элемент под курсором
        HelpProvider.SetHelpKey(this, "SettingsProtocol");
      };
    }
  }
}
