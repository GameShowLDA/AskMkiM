using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AppConfig.Config.SystemStateManager;

namespace Mode.Settings.Execution
{
  partial class ExecutionControl
  {
    /// <summary>
    /// Обрабатывает событие установки флажка задержки выполнения.
    /// Вызывает метод сохранения новых данных.
    /// </summary>
    private async void ExecutionDelay_Checked(object sender, RoutedEventArgs e)
    {
      await NewDataSaveAsync();
    }

    /// <summary>
    /// Обрабатывает событие установки флажка режима ожидания.
    /// Вызывает метод сохранения новых данных.
    /// </summary>
    private async void IdleMode_Checked(object sender, RoutedEventArgs e)
    {
      if (await GetIsActivePower() && (sender as CheckBox).IsChecked == true)
      {
        MessageBox.Show("Отключите питание системы для перехода в холостой режим!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        (sender as CheckBox).IsChecked = !(sender as CheckBox).IsChecked;
        return;
      }
      else
      {
        await NewDataSaveAsync();
      }
    }

    /// <summary>
    /// Обрабатывает событие предварительного ввода текста в текстовое поле.
    /// Проверяет, является ли вводимый текст числовым.
    /// </summary>
    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (e.Text == "." || e.Text == ",")
      {
        e.Handled = true;

        var textBox = sender as TextBox;
        if (textBox != null)
        {
          int cursorPosition = textBox.SelectionStart;
          textBox.Text = textBox.Text.Insert(cursorPosition, ",");

          textBox.SelectionStart = cursorPosition + 1;
          textBox.SelectionLength = 0;
        }
      }
      else
      {
        CheckIsNumeric(e);
      }
    }

    ///// <summary>
    ///// Обрабатывает событие изменения текста.
    ///// Вызывает метод сохранения новых данных.
    ///// </summary>
    //private async void TextChanged(object sender, TextChangedEventArgs e)
    //{
    //  await NewDataSaveAsync();
    //}

    /// <summary>
    /// Обрабатывает событие установки флажка пошагового выполнения.
    /// Вызывает метод сохранения новых данных.
    /// </summary>
    private async void StepByStep_Checked(object sender, RoutedEventArgs e)
    {
      await NewDataSaveAsync();
    }
  }
}
