using Ask.Core.Services.Usb;
using System.Windows.Controls;
using System.Windows.Input;

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
  }

}
