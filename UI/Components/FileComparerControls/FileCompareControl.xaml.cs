using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareControl.xaml
  /// </summary>
  public partial class FileCompareControl : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }
    public FileCompareControl(string firstFilePath, string secondFilePath)
    {
      InitializeComponent();
      this.FirstFilePath = firstFilePath;
      this.SecondFilePath = secondFilePath;
      LoadFiles();
    }

    private void LoadFiles()
    {
      var firstFileText = File.ReadAllText(this.FirstFilePath);
      var secondFileText = File.ReadAllText(this.SecondFilePath);
      if (HorizontalPanel.Visibility == Visibility.Visible)
      {
        TopBox.Text = firstFileText;
        BottomBox.Text = secondFileText;
      }
      else
      {
        LeftBox.Text = firstFileText;
        RightBox.Text = secondFileText;
      }
    }

    private void ToggleOrientation(object sender, MouseButtonEventArgs e)
    {
      bool toVertical = sender == LeftRight;

      HorizontalPanel.Visibility = toVertical ? Visibility.Collapsed : Visibility.Visible;
      VerticalPanel.Visibility = toVertical ? Visibility.Visible : Visibility.Collapsed;

      UpDown.Visibility = toVertical ? Visibility.Visible : Visibility.Collapsed;
      LeftRight.Visibility = toVertical ? Visibility.Collapsed : Visibility.Visible;

      if (HorizontalPanel.Visibility == Visibility.Visible)
      {
        (TopBox.Text, BottomBox.Text) = (LeftBox.Text, RightBox.Text);
      }
      else
      {
        (LeftBox.Text, RightBox.Text) = (TopBox.Text, BottomBox.Text);
      }
    }
  }
}
