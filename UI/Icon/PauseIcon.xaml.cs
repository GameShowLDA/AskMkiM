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

namespace UI.Icon
{
  /// <summary>
  /// Логика взаимодействия для PauseIcon.xaml
  /// </summary>
  public partial class PauseIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
               DependencyProperty.Register(nameof(Size), typeof(double), typeof(PauseIcon),
                   new PropertyMetadata(24.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public PauseIcon()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PauseIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}
