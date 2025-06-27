using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.Base;
using static AppConfiguration.SystemState.SystemStateManager;
using static UI.Controls.Message.MessageBox;


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
        Show(Status.Error, "Отключите питание системы для перехода в холостой режим!", "Ошибка", MessageBoxButton.OK);
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

    /// <summary>
    /// Обрабатывает событие установки флажка пошагового выполнения.
    /// Вызывает метод сохранения новых данных.
    /// </summary>
    private async void StepByStep_Checked(object sender, RoutedEventArgs e)
    {
      await NewDataSaveAsync();
    }

    private void debugError_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
      var control = sender as CheckBox;
      if ((bool)control.IsChecked)
      {
        AppConfiguration.Admin.AdminConfig.ErrorDebug = true;
      }
      else
      {
        AppConfiguration.Admin.AdminConfig.ErrorDebug = false;
      }
    }

    private void EventAggregator_AdminRightsChanged(bool obj)
    {
      if (obj)
      {
        AdminPanel.Visibility = Visibility.Visible;
      }
      else
      {
        AdminPanel.Visibility = Visibility.Collapsed;
        debugError.IsChecked = false;
      }
    }

    private void ExecutionControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
      EventAggregator.AdminRightsChanged += EventAggregator_AdminRightsChanged;

      if (EventAggregator.GetAdminRights())
      {
        EventAggregator_AdminRightsChanged(true);
      }
    }
  }
}
