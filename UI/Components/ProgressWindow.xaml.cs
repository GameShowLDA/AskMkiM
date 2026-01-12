using System.Windows;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для ProgressWindow.xaml.
  /// </summary>
  public partial class ProgressWindow : Window
  {
    /// <summary>
    /// Конструктор окна прогресса.
    /// Инициализирует компоненты окна прогресса.
    /// </summary>
    public ProgressWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Устанавливает значение прогресса на прогресс-баре.
    /// </summary>
    /// <param name="value">Значение прогресса от 0 до 100.</param>
    public void SetProgress(double value)
    {
      progressBar.IsIndeterminate = false;
      progressBar.Value = value;
    }
  }
}
