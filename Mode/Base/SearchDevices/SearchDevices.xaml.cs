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

namespace Mode.Base.SearchDevices
{
  /// <summary>
  /// Логика взаимодействия для SearchDevices.xaml
  /// </summary>
  public partial class SearchDevices : Window
  {
    public SearchDevices()
    {
      InitializeComponent();
    }

    public void SetDescription(string text)
    {
      header.Text = text;
    }
  }
}
