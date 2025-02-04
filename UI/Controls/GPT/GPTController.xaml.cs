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

namespace UI.Controls.GPT
{
  /// <summary>
  /// Логика взаимодействия для GPTController.xaml
  /// </summary>
  public partial class GPTController : UserControl
  {
    public GPTController()
    {
      InitializeComponent();
      DataContext = this;
    }

    public object SelectedModeContent { get; set; }

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
      if (sender is RadioButton radioButton)
      {
        // Определяем, какой режим выбран
        switch (radioButton.Tag as string)
        {
          case "Mode1":
            Content.Children.Clear();
            Content.Children.Add(new Mode.AcwMode());
            break;

          case "Mode2":
            Content.Children.Clear();
            Content.Children.Add(new Mode.DcwMode());
            break;

          case "Mode3":
            Content.Children.Clear();
            Content.Children.Add(new Mode.IrMode());
            break;

          case "Mode4":
            Content.Children.Clear();
            Content.Children.Add(new Mode.SettingsGPT());
            break;
        }
      }
    }


  }
}
