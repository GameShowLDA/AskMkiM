using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Windows
{
  /// <summary>
  /// Логика взаимодействия для VoltageValue.xaml
  /// </summary>
  public partial class VoltageValue : Window, IReferenceVoltageRequestService
  {
    /// <summary>
    /// Результат введённого напряжения.
    /// </summary>
    public double VoltageResult { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="VoltageValue"/>.
    /// </summary>
    public VoltageValue()
    {
      InitializeComponent();
      this.Loaded += Window_Loaded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      VoltageInput.Focus();
    }

    /// <summary>
    /// Ограничивает ввод только числовыми значениями для номера устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void NumberDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = sender as TextBox;
      if (textBox == null)
      {
        e.Handled = true;
        return;
      }

      if (e.Text == ".")
      {
        int caretIndex = textBox.CaretIndex;
        textBox.Text = textBox.Text.Insert(caretIndex, ",");
        textBox.CaretIndex = caretIndex + 1;
        e.Handled = true;
        return;
      }

      e.Handled = !Regex.IsMatch(e.Text, @"^[0-9,]$");

      if (!e.Handled && e.Text == "," && textBox.Text.Contains(","))
      {
        e.Handled = true;
      }
    }

    private void VoltageInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        SaveButton_Click(sender, e);
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      if (double.TryParse(VoltageInput.Text, out double voltage))
      {
        VoltageResult = voltage;
        this.DialogResult = true;
        this.Close();
      }
      else
      {
        Message.MessageBoxCustom.Show($"Не удалось распознать параметр: {VoltageInput.Text}. Повторите попытку ввода данных!", "Ошибка данных", image: MessageBoxImage.Error);
      }
    }

    public async Task<double?> RequestReferenceVoltageAsync(UserControl userMessageService)
    {
      var result = await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        VoltageValue chassisManagerWindow = new VoltageValue();
        userMessageService.Effect = new System.Windows.Media.Effects.BlurEffect();

        bool? dialogResult = chassisManagerWindow.ShowDialog();
        userMessageService.Effect = null;

        if (dialogResult == true)
        {
          return chassisManagerWindow.VoltageResult;
        }
        else
        {
          return -1;
        }
      });
      return result;
    }
  }
}
