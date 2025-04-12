using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;

namespace UI.Components.FileComparerControls
{

  public class FileCompare
  {
    public static List<Dictionary<int, string>> CompareFileContents(string path1, string path2)
    {
      var lines1 = File.ReadAllLines(path1);
      var lines2 = File.ReadAllLines(path2);
      int maxLines = Math.Max(lines1.Length, lines2.Length);

      int rowIndex = 0;
      bool insideDifferenceBlock = false;
      int blockStartRow = 0;
      int diffCountFirst = 0; // Подсчет различий в блоке
      int diffCountSecond = 0; // Подсчет различий в блоке
      var differenceFirstFile = new Dictionary<int, string>();
      var differencesecondFile = new Dictionary<int, string>();

      for (int i = 0; i < maxLines; i++)
      {
        var newIndexFirst = i + diffCountFirst;
        var newIndexSecond = i + diffCountSecond;
        if (newIndexFirst > lines1.Length)
        {
          newIndexFirst = lines1.Length - 1;
        }
        if (newIndexSecond > lines2.Length)
        {
          newIndexSecond = lines2.Length - 1;
        }
        string line1 = i < lines1.Length ? lines1[newIndexFirst] : string.Empty;
        string line2 = i < lines2.Length ? lines2[newIndexSecond] : string.Empty;
        bool isDifferent = !line1.Equals(line2);
        if (string.IsNullOrEmpty(line1) && i < lines1.Length)
        {
          line1 = lines1[newIndexFirst + 1];
          var temp = line1.Equals(line2);

          if (temp)
          {
            diffCountFirst++;
          }
          else
          {
            differenceFirstFile.Add(newIndexFirst, lines1[newIndexFirst]);
          }
        }
        if (string.IsNullOrEmpty(line2) && i < lines2.Length)
        {
          line2 = lines2[newIndexSecond + 1];
          var temp = line1.Equals(line2);

          if (temp)
          {
            diffCountSecond++;
          }
          else
          {
            differenceFirstFile.Add(newIndexFirst, lines2[newIndexSecond]);
          }
        }
      }
      return new List<Dictionary<int, string>> { differenceFirstFile, differencesecondFile };
    }

    private static void AddBorder(Grid grid, int startRow, int endRow)
    {
      Border border = new Border
      {
        BorderBrush = Brushes.Green,
        BorderThickness = new Thickness(2),
        Margin = new Thickness(2)
      };

      Grid.SetRow(border, startRow);
      Grid.SetRowSpan(border, endRow - startRow + 1);
      Grid.SetColumn(border, 0);
      Grid.SetColumnSpan(border, 4);

      grid.Children.Add(border);
    }

    private static bool IsContextLine(string[] lines1, string[] lines2, int index, int contextLines)
    {
      for (int i = Math.Max(0, index - contextLines); i <= Math.Min(lines1.Length - 1, index + contextLines); i++)
      {
        if (i < lines1.Length && i < lines2.Length && !lines1[i].Equals(lines2[i]))
        {
          return true;
        }
      }
      return false;
    }

    private static void AddRow(Grid grid, int lineNum1, string text1, int lineNum2, string text2, bool isDifferent, int rowIndex)
    {
      grid.RowDefinitions.Add(new RowDefinition());

      // Номер строки для первого файла
      TextBlock lineNumber1 = new TextBlock
      {
        Text = lineNum1.ToString(),
        Foreground = Brushes.Gray,
        FontFamily = new FontFamily("Consolas"),
        Margin = new Thickness(5),
        VerticalAlignment = VerticalAlignment.Top
      };

      // Текст строки первого файла
      TextBlock tb1 = new TextBlock
      {
        Text = text1,
        Background = isDifferent ? new SolidColorBrush(Color.FromRgb(255, 200, 200)) : Brushes.Transparent, // Мягкий красный фон
        Foreground = Brushes.Black, // Чёрный текст для контраста
        FontFamily = new FontFamily("Consolas"),
        FontSize = 14, // Увеличенный размер шрифта
        Margin = new Thickness(5),
        TextWrapping = TextWrapping.Wrap
      };

      // Номер строки для второго файла
      TextBlock lineNumber2 = new TextBlock
      {
        Text = lineNum2.ToString(),
        Foreground = Brushes.Gray,
        FontFamily = new FontFamily("Consolas"),
        Margin = new Thickness(5),
        VerticalAlignment = VerticalAlignment.Top
      };

      // Текст строки второго файла
      TextBlock tb2 = new TextBlock
      {
        Text = text2,
        Background = isDifferent ? new SolidColorBrush(Color.FromRgb(200, 255, 200)) : Brushes.Transparent, // Мягкий зелёный фон
        Foreground = Brushes.Black,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 14, // Увеличенный размер шрифта
        Margin = new Thickness(5),
        TextWrapping = TextWrapping.Wrap
      };

      Grid.SetRow(lineNumber1, rowIndex);
      Grid.SetColumn(lineNumber1, 0);
      Grid.SetRow(tb1, rowIndex);
      Grid.SetColumn(tb1, 1);
      Grid.SetRow(lineNumber2, rowIndex);
      Grid.SetColumn(lineNumber2, 2);
      Grid.SetRow(tb2, rowIndex);
      Grid.SetColumn(tb2, 3);

      grid.Children.Add(lineNumber1);
      grid.Children.Add(tb1);
      grid.Children.Add(lineNumber2);
      grid.Children.Add(tb2);
    }
  }
}