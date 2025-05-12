using System.Windows;
using Mode.Base;
using static NewCore.Enum.MetrologyEnum;
using UI.Controls.Protocol;
using Utilities.Models;

namespace TestWPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      InitializeSettings();
    }


    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      Test.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      Task.Run(async () =>
      {
        int i = 0;
        while (true)
        {
          await Test.ShowMessageAsync(new ShowMessageModel("Тест", message: i.ToString() + $"[{ShowMessageModel.SuccessMessage.Title}]", messageColor: ShowMessageModel.SuccessMessage.TitleColor));
          i++;

          await Task.Delay(10);
        }
      });
    }


  }
}
