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

namespace TestWPF
{
  /// <summary>
  /// Логика взаимодействия для TestMainText.xaml
  /// </summary>
  public partial class TestMainText : UserControl
  {
    public TestMainText()
    {
      InitializeComponent();

      textEditorControl.InitializeMarkerService();
      //textEditorControl.HighlightText("а");

      HighlightAllB();
    }

    private void HighlightAllB()
    {
      string text = textEditorControl.Text;
      var ranges = new List<(int, int)>();

      for (int i = 0; i < text.Length; i++)
      {
        if (text[i] == 'б' || text[i] == 'Б') // поддержка и заглавной, и строчной
        {
          ranges.Add((i, i + 1));
        }
      }

      Console.WriteLine($"🔍 Найдено {ranges.Count} вхождений 'б'.");

      textEditorControl.HighlightRanges(ranges);
    }

  }
}
