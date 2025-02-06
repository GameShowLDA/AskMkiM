using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Логика взаимодействия для GPTPunchControl.xaml
  /// </summary>
  public partial class GPTPunchControl : UserControl
  {

    static internal Core.GptLibrary.Model ModelGPT { get; set; }
    public GPTPunchControl()
    {
      InitializeComponent();
    }
    private void ConnectMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      ModelGPT = new Core.GptLibrary.Model();
      ModelGPT.Connect();
      if (ModelGPT.CheckConnection())
      {
        ConnectMenuItem.Visibility = Visibility.Collapsed;
        DisconnectMenuItem.Visibility = Visibility.Visible;
        Controller.Visibility = Visibility.Visible;
      }
    }

    private void DisconnectMenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ModelGPT.CheckConnection())
      {
        ModelGPT.Disconnect();
        ConnectMenuItem.Visibility = Visibility.Visible;
        DisconnectMenuItem.Visibility = Visibility.Collapsed;
        Controller.Visibility = Visibility.Collapsed;
      }
    }
  }
}
