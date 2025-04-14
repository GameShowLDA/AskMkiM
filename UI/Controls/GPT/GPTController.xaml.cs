using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT
{
  /// <summary>
  /// Логика взаимодействия для GPTController.xaml.
  /// </summary>
  public partial class GPTController : UserControl
  {
    /// <summary>
    /// Контроллер для управления режимами GPT.
    /// </summary>
    public GPTController()
    {
      InitializeComponent();
      DataContext = this;
    }

    /// <summary>
    /// Получает или устанавливает выбранный контент режима.
    /// </summary>
    public object SelectedModeContent { get; set; }

    /// <summary>
    /// Обрабатывает событие выбора режима.
    /// В зависимости от выбранного режима загружает соответствующий элемент управления в контейнер.
    /// </summary>
    /// <param name="sender">Источник события, обычно радио кнопка.</param>
    /// <param name="e">Данные события.</param>
    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
      if (sender is RadioButton radioButton)
      {
        // Определяем, какой режим выбран
        switch (radioButton.Tag as string)
        {
          case "Mode1":
            Content.Children.Clear();
            Content.Children.Add(new Mode.AcwMode());
            break;

          case "Mode2":
            Content.Children.Clear();
            Content.Children.Add(new Mode.DcwMode());
            break;

          case "Mode3":
            Content.Children.Clear();
            Content.Children.Add(new Mode.IrMode());
            break;

          case "Mode4":
            Content.Children.Clear();
            Content.Children.Add(new Mode.SettingsGPT());
            break;
        }
      }
    }
  }
}
