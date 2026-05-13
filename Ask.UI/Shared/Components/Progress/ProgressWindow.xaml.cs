using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.Shared.Components.Progress
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
      UpdateUi(() =>
      {
        progressBar.IsIndeterminate = false;
        progressBar.Value = value;
      });
    }

    public void Configure(string title, string status, string hint)
    {
      UpdateUi(() =>
      {
        TitleTextBlock.Text = string.IsNullOrWhiteSpace(title)
          ? "Выполняется операция"
          : title;

        StatusTextBlock.Text = string.IsNullOrWhiteSpace(status)
          ? "Подготовка..."
          : status;

        HintTextBlock.Text = string.IsNullOrWhiteSpace(hint)
          ? string.Empty
          : hint;
      });
    }

    public void SetStage(string status, string? hint = null)
    {
      UpdateUi(() =>
      {
        StatusTextBlock.Text = string.IsNullOrWhiteSpace(status)
          ? "Подготовка..."
          : status;

        if (hint != null)
        {
          HintTextBlock.Text = string.IsNullOrWhiteSpace(hint)
            ? string.Empty
            : hint;
        }
      });
    }

    public void SetStatus(string status)
    {
      SetStage(status);
    }

    public void SetHint(string hint)
    {
      UpdateUi(() =>
      {
        HintTextBlock.Text = string.IsNullOrWhiteSpace(hint)
          ? string.Empty
          : hint;
      });
    }

    private void UpdateUi(Action action)
    {
      if (Dispatcher.CheckAccess())
      {
        action();
        return;
      }

      _ = Dispatcher.InvokeAsync(action, DispatcherPriority.Background);
    }
  }
}
