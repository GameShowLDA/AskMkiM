using Ask.DataBase.Engine.Static.Devices;
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

namespace UI.Controls.Keysight
{
  /// <summary>
  /// Логика взаимодействия для KeysightPunchControl.xaml
  /// </summary>
  public partial class KeysightPunchControl : UserControl
  {
    public KeysightPunchControl()
    {
      InitializeComponent();
    }

    private async void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var meter = await FastMeters.GetDeviceByNumberChassisAsync(1, 16);
      await meter.TextMessage.Message(TextMessageControl.Text);
    }
  }
}
