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
using System.Windows.Shapes;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для ProgressWindow.xaml
  /// </summary>
  public partial class ProgressWindow : Window
  {
    public ProgressWindow()
    {
      InitializeComponent();
    }

    public void SetProgress(double value)
    {
      progressBar.IsIndeterminate = false;
      progressBar.Value = value;
    }
  }
}
