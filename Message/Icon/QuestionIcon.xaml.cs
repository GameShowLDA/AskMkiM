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

namespace Message.Icon
{
  /// <summary>
  /// Логика взаимодействия для QuestionIcon.xaml
  /// </summary>
  public partial class QuestionIcon : UserControl
  {
    public QuestionIcon()
    {
      InitializeComponent();
      Width = Size;
      Height = Size;
    }

    public static readonly DependencyProperty SizeProperty =
       DependencyProperty.Register(nameof(Size), typeof(double), typeof(QuestionIcon),
           new PropertyMetadata(64.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is QuestionIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

    public static readonly DependencyProperty CircleColorProperty =
        DependencyProperty.Register(nameof(CircleColor), typeof(Brush), typeof(QuestionIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue)));

    public Brush CircleColor
    {
      get => (Brush)GetValue(CircleColorProperty);
      set => SetValue(CircleColorProperty, value);
    }

    public static readonly DependencyProperty IconFillColorProperty =
        DependencyProperty.Register(nameof(IconStrokeColor), typeof(Brush), typeof(QuestionIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.White)));

    public Brush IconStrokeColor
    {
      get => (Brush)GetValue(IconFillColorProperty);
      set => SetValue(IconFillColorProperty, value);
    }

    public Brush GetCircleColor() => CircleColor;

    public Brush GetIconFillColor() => IconStrokeColor;
  }
}
