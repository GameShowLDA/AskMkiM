using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AppConfiguration.Base;
using AppConfiguration.Execution;
using AppConfiguration.MeasurementError;
using AppConfiguration.Protocol;
using AppConfiguration.Theme;
using DataBaseConfiguration;
using UI.Controls.Protocol;
using UI.Controls.ProtocolController;
using Utilities.Models;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;

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
      InitializeTestProtocol();
    }

    /// Инициализирует тестовый бесконечный вывод в Protocol.
    /// </summary>
    private void InitializeTestProtocol()
    {
      int counter = 0;

      // Делегат запуска
      StartDelegate start = async (CancellationToken token) =>
      {
        while (!token.IsCancellationRequested)
        {
          counter++;

          // Вход в блок на каждом 100-м шаге
          if (counter % 100 == 1)
            testProtocol.StepManager.EnterBlock();

          await testProtocol.Message.AppendLineAsync(new ShowMessageModel(
            $"Строка № {counter}",
            Colors.LightGreen
          ));

          if (counter % 100 == 0)
            await testProtocol.StepManager.ExitBlockAsync();
        }
      };

      // Делегат остановки (можно оставить пустым или логировать)
      StopDelegate stop = async (CancellationToken token) =>
      {
        await testProtocol.Message.AppendLineAsync(new ShowMessageModel(
          "Процесс остановлен пользователем.",
          ShowMessageModel.ErrorMessage.TitleColor
        ));
      };

      // Устанавливаем настройки и подключаем к контролу
      var settings = new ProtocolExecutionSettings(this, start)
      {
        StopDelegate = stop,
        IsRepeatEnabled = false
      };

      testProtocol.SetSettings(settings);
    }
  }
}
