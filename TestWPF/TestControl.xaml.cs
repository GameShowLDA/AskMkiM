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
using UI.Icon;

namespace TestWPF
{
  /// <summary>
  /// Логика взаимодействия для TestControl.xaml
  /// </summary>
  public partial class TestControl : UserControl
  {
    public new Brush Background
    {
      get
      {
        return TestBorder.Background;
      }
      set
      { 
        TestBorder.Background = value;
      }
    }

    public TestControl()
    {
      InitializeComponent();
    }
  }
}
