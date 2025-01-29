using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для WindowButtons.xaml
  /// </summary>
  public partial class WindowButtons : UserControl
  {
    /// <summary>
    /// Перечисление, представляющее варианты выбора для кнопок окна
    /// </summary>
    public enum Choice
    {
      Exit,
      Minimize,
      Maximize,
      None
    }

    /// <summary>
    /// Свойство, определяющее текущий выбранный элемент кнопки окна
    /// </summary>
    public Choice ChoiceElement { get; set; }

    /// <summary>
    /// Переопределенный метод для отрисовки содержимого элемента управления
    /// </summary>
    /// <param name="dc">Контекст отрисовки</param>
    protected override void OnRender(DrawingContext dc)
    {
      base.OnRender(dc);

      if (ActualWidth <= 0 || ActualHeight <= 0)
        return;

      Pen pen = new Pen(Foreground, 1);

      double widthRect = ActualWidth / 4;
      double heightRect = ActualHeight / 4;
      double locationX = (ActualWidth - widthRect) / 2;
      double locationY = (ActualHeight - heightRect) / 2;

      if (ChoiceElement == Choice.Exit)
      {
        dc.DrawLine(pen, new Point(locationX, locationY), new Point(widthRect + locationX, heightRect + locationY));
        dc.DrawLine(pen, new Point(widthRect + locationX, locationY), new Point(locationX, heightRect + locationY));
      }
      else if (ChoiceElement == Choice.Minimize)
      {
        dc.DrawLine(pen, new Point(locationX, ActualWidth / 2), new Point(locationX + widthRect, ActualWidth / 2));
      }
      else if (ChoiceElement == Choice.Maximize)
      {
        dc.DrawRectangle(null, pen, new Rect(locationX, locationY, widthRect, heightRect));

      }
    }

    /// <summary>
    /// Переопределенный метод, вызываемый при изменении размера элемента управления
    /// </summary>
    /// <param name="sizeInfo">Информация об изменении размера</param>
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
      base.OnRenderSizeChanged(sizeInfo);
      Width = Height = sizeInfo.NewSize.Width < sizeInfo.NewSize.Height ? sizeInfo.NewSize.Width : sizeInfo.NewSize.Height;
      InvalidateVisual();
    }

    /// <summary>
    /// Конструктор класса WindowButtons
    /// </summary>
    public WindowButtons()
    {
      InitializeComponent();
      ChoiceElement = Choice.None;
      Width = Height = 20;
      Cursor = Cursors.Hand;
    }
  }
}
