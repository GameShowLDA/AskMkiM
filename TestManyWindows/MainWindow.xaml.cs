using System.Windows;
using System.Windows.Input;

namespace TestDocking
{
  public partial class MainWindow : Window
  {
    private bool isLeftPanelVisible = true;

    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public MainWindow()
    {
      InitializeComponent();
    }

    private async void MenuButton_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      isLeftPanelVisible = !isLeftPanelVisible;

      if (!isLeftPanelVisible)
      {
        int newWidth = 50;
        while (PanelManagment.Width.Value > newWidth)
        {
          PanelManagment.Width = new GridLength(PanelManagment.Width.Value - 25);
          if (ButtonsGrid.Opacity > 0)
          {
            ButtonsGrid.Opacity -= 0.1;
          }
          await Task.Delay(1);
        }
        ButtonsGrid.Opacity = 0;
      }
      else
      {
        int newWidth = 250;
        while (PanelManagment.Width.Value < newWidth)
        {
          PanelManagment.Width = new System.Windows.GridLength(PanelManagment.Width.Value + 25);
          if (ButtonsGrid.Opacity < 1)
          {
            ButtonsGrid.Opacity += 0.1;
          }
          await Task.Delay(1);
        }

        PanelManagment.Width = new GridLength(250);
        ButtonsGrid.Opacity = 1;
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
        (TopBox.Text, BottomBox.Text, FirstFileName.Text, SecondFileName.Text) =
          (LeftBox.Text, RightBox.Text, FirstVerticalFileName.Text, SecondVerticalFileName.Text);
      }
      else
      {
        (LeftBox.Text, RightBox.Text, FirstVerticalFileName.Text, SecondVerticalFileName.Text) =
          (TopBox.Text, BottomBox.Text, FirstFileName.Text, SecondFileName.Text);
      }
    }
  }
}
