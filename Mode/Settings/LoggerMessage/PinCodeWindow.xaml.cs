using System.Windows;

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
