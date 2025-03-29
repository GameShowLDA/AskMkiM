using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mode.Metrology.PI
{
  /// <summary>
  /// Логика взаимодействия для VoltageValue.xaml.
  /// </summary>
  public partial class VoltageValue : Window
  {
    /// <summary>
    /// Результат введённого напряжения.
    /// </summary>
    public string VoltageResult { get; private set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="VoltageValue"/>.
    /// </summary>
    public VoltageValue()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Ограничивает ввод только числовыми значениями для номера устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void NumberDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      VoltageResult = VoltageInput.Text;
      this.DialogResult = true;
      this.Close();
    }
  }
}
