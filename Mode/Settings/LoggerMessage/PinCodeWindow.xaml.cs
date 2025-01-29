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

namespace Mode.Settings.LoggerMessage
{
  /// <summary>
  /// Логика взаимодействия для PinCodeWindow.xaml
  /// </summary>
  public partial class PinCodeWindow : Window
  {
    public bool IsCorrectPin { get; private set; }
    public PinCodeWindow()
    {
      InitializeComponent();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
      if (PinCodeBox.Password == "0000")
      {
        IsCorrectPin = true;
        Close();
      }
      else
      {
        MessageBox.Show("Неверный PIN-код. Попробуйте еще раз.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        PinCodeBox.Clear();
      }
    }
  }
}
