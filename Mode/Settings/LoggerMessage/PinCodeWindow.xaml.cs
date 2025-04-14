using System.Windows;

namespace Mode.Settings.LoggerMessage
{
  /// <summary>
  /// Окно для ввода PIN-кода.
  /// </summary>
  public partial class PinCodeWindow : Window
  {
    /// <summary>
    /// Получает значение, указывающее, является ли введённый PIN-код правильным.
    /// </summary>
    public bool IsCorrectPin { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PinCodeWindow"/>.
    /// </summary>
    public PinCodeWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки подтверждения PIN-кода.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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
