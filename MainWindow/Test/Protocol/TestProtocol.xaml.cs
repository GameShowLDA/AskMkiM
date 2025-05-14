using System.Windows.Controls;
using System.Windows.Media;
using Utilities.Models;

namespace MainWindowProgram.Test.Protocol
{
  /// <summary>
  /// Логика взаимодействия для TestProtocol.xaml
  /// </summary>
  public partial class TestProtocol : UserControl
  {

    public TestProtocol()
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
      await Task.Run(async () =>
       {
         int i = 0;
         while (true)
         {
           Test.GetCancellationToken().ThrowIfCancellationRequested();

           if (i % 100 == 0)
           {
             await Test.ShowMessageAsync(new ShowMessageModel($"БЛОК {i / 100 + 1}", message: "Вложенный шаг", messageColor: Colors.Yellow), true);
           }

           await Test.ShowMessageAsync(new ShowMessageModel("Тест", message: i.ToString() + $"[{ShowMessageModel.SuccessMessage.Title}]", messageColor: ShowMessageModel.SuccessMessage.TitleColor));
           i++;
         }
       });
    }
  }
}
