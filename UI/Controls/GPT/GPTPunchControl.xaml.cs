using Ask.Core.Services.App;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Контрол для управления режимом GPTPunch.
  /// </summary>
  public partial class GPTPunchControl : UserControl
  {
    /// <summary>
    /// Статическая модель GPT, используемая для подключения и проверки связи.
    /// </summary>
    static internal IBreakdownTester? ModelGPT { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GPTPunchControl"/>.
    /// </summary>
    public GPTPunchControl()
    {
      InitializeComponent();
      ModelGPT = ServiceLocator.GetRequired<BreakdownTesterServices>().GetDevicesByNumberChassis(1).FirstOrDefault();
      Controller.Visibility = Visibility.Visible;
    }
  }
}
