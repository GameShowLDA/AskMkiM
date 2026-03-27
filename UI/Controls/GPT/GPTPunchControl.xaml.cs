using Ask.Core.Services.App;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.DataBase.Engine.Static.Devices;
using DataBaseConfiguration.Services.Device;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
      ModelGPT = BreakdownTesters.GetDevicesByNumberChassisAsync(1).GetAwaiter().GetResult().FirstOrDefault();
      Controller.Visibility = Visibility.Visible;
    }
  }
}
