using System.Windows.Controls;
using System.Windows.Media;

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
