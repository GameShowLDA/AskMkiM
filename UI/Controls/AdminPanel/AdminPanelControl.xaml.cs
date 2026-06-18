using Ask.Core.Services.Usb;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.DataBase.Engine.Static.Devices;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.GPT;
using UI.Controls.Keysight;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Логика взаимодействия для AdminPanelControl.xaml
  /// </summary>
  public partial class AdminPanelControl : UserControl
  {
    public AdminPanelControl()
    {
      InitializeComponent();
    }

    private void DatabaseButton_Click(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new DataBaseView();
    }

    private void UsbButton_Click(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new USBManagementControl();
    }

    private void SetCommandButton_Click(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new SetCommand();
    }

    private void ResistanceButton_Click(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new CheckResistanceControl();
    }

    private void Gpt_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new GPTPunchControl();
    }

    private async void Meter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RightContentPresenter.Content = new KeysightPunchControl();
    }
  }

}
